use super::*;
use std::sync::{Arc, Mutex};
use std::collections::VecDeque;
use tokio::sync::Mutex as AsyncMutex;

/// Mock transport for testing
struct MockTransport {
    config: TransportConfig,
    state: ConnectionState,
    stats: NetworkStats,
    send_buffer: Arc<AsyncMutex<VecDeque<TransportMessage>>>,
    receive_buffer: Arc<AsyncMutex<VecDeque<TransportMessage>>>,
    should_fail: bool,
    fail_after_messages: Option<usize>,
    messages_sent: usize,
}

impl MockTransport {
    fn new(config: TransportConfig) -> Self {
        Self {
            config,
            state: ConnectionState::Disconnected,
            stats: NetworkStats::default(),
            send_buffer: Arc::new(AsyncMutex::new(VecDeque::new())),
            receive_buffer: Arc::new(AsyncMutex::new(VecDeque::new())),
            should_fail: false,
            fail_after_messages: None,
            messages_sent: 0,
        }
    }
    
    fn with_failure(mut self) -> Self {
        self.should_fail = true;
        self
    }
    
    fn fail_after(mut self, count: usize) -> Self {
        self.fail_after_messages = Some(count);
        self
    }
    
    async fn inject_message(&self, message: TransportMessage) {
        self.receive_buffer.lock().await.push_back(message);
    }
}

#[async_trait::async_trait]
impl Transport for MockTransport {
    async fn start(&mut self) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::TransportError("Mock start failure".to_string()));
        }
        self.state = ConnectionState::Disconnected;
        Ok(())
    }
    
    async fn connect(&mut self, _addr: SocketAddr) -> Result<()> {
        if self.should_fail {
            self.state = ConnectionState::Failed;
            return Err(RemoteCError::TransportError("Mock connection failure".to_string()));
        }
        
        self.state = ConnectionState::Connecting;
        tokio::time::sleep(tokio::time::Duration::from_millis(10)).await;
        self.state = ConnectionState::Connected;
        self.stats.rtt_ms = 5.0;
        Ok(())
    }
    
    async fn accept(&mut self) -> Result<SocketAddr> {
        if self.should_fail {
            return Err(RemoteCError::TransportError("Mock accept failure".to_string()));
        }
        
        self.state = ConnectionState::Connected;
        Ok("127.0.0.1:12345".parse().unwrap())
    }
    
    async fn send(&mut self, message: TransportMessage) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::TransportError("Mock send failure".to_string()));
        }
        
        if let Some(fail_after) = self.fail_after_messages {
            if self.messages_sent >= fail_after {
                self.state = ConnectionState::Failed;
                return Err(RemoteCError::TransportError("Simulated send failure".to_string()));
            }
        }
        
        // Update stats
        match &message {
            TransportMessage::VideoFrame { data, .. } => {
                self.stats.bytes_sent += data.len() as u64;
            }
            TransportMessage::AudioData { data, .. } => {
                self.stats.bytes_sent += data.len() as u64;
            }
            TransportMessage::InputEvent { event_data, .. } => {
                self.stats.bytes_sent += event_data.len() as u64;
            }
            TransportMessage::Control { payload, .. } => {
                self.stats.bytes_sent += payload.len() as u64;
            }
            TransportMessage::Heartbeat { .. } => {
                self.stats.bytes_sent += 8; // timestamp size
            }
        }
        
        self.stats.packets_sent += 1;
        self.messages_sent += 1;
        
        self.send_buffer.lock().await.push_back(message);
        Ok(())
    }
    
    async fn receive(&mut self) -> Result<TransportMessage> {
        if self.should_fail {
            return Err(RemoteCError::TransportError("Mock receive failure".to_string()));
        }
        
        let mut buffer = self.receive_buffer.lock().await;
        if let Some(message) = buffer.pop_front() {
            // Update stats
            match &message {
                TransportMessage::VideoFrame { data, .. } => {
                    self.stats.bytes_received += data.len() as u64;
                }
                TransportMessage::AudioData { data, .. } => {
                    self.stats.bytes_received += data.len() as u64;
                }
                TransportMessage::InputEvent { event_data, .. } => {
                    self.stats.bytes_received += event_data.len() as u64;
                }
                TransportMessage::Control { payload, .. } => {
                    self.stats.bytes_received += payload.len() as u64;
                }
                TransportMessage::Heartbeat { .. } => {
                    self.stats.bytes_received += 8;
                }
            }
            self.stats.packets_received += 1;
            Ok(message)
        } else {
            Err(RemoteCError::TransportError("No messages available".to_string()))
        }
    }
    
    fn state(&self) -> ConnectionState {
        self.state
    }
    
    fn stats(&self) -> NetworkStats {
        self.stats.clone()
    }
    
    async fn close(&mut self) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::TransportError("Mock close failure".to_string()));
        }
        self.state = ConnectionState::Closed;
        Ok(())
    }
}

#[test]
fn test_transport_config_default() {
    let config = TransportConfig::default();
    assert_eq!(config.protocol, TransportProtocol::Quic);
    assert_eq!(config.mtu, 1200);
    assert_eq!(config.connect_timeout, Duration::from_secs(10));
    assert!(config.congestion_control);
}

#[tokio::test]
async fn test_connection_lifecycle() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    // Initial state
    assert_eq!(transport.state(), ConnectionState::Disconnected);
    
    // Start transport
    transport.start().await.unwrap();
    assert_eq!(transport.state(), ConnectionState::Disconnected);
    
    // Connect
    let addr = "127.0.0.1:8080".parse().unwrap();
    transport.connect(addr).await.unwrap();
    assert_eq!(transport.state(), ConnectionState::Connected);
    
    // Close
    transport.close().await.unwrap();
    assert_eq!(transport.state(), ConnectionState::Closed);
}

#[tokio::test]
async fn test_connection_failure() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config).with_failure();
    
    let addr = "127.0.0.1:8080".parse().unwrap();
    let result = transport.connect(addr).await;
    assert!(result.is_err());
    assert_eq!(transport.state(), ConnectionState::Failed);
}

#[tokio::test]
async fn test_send_video_frame() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    transport.connect("127.0.0.1:8080".parse().unwrap()).await.unwrap();
    
    let frame_data = Bytes::from(vec![0u8; 1000]);
    let message = TransportMessage::VideoFrame {
        sequence: 1,
        timestamp: 1000,
        is_keyframe: true,
        data: frame_data,
    };
    
    transport.send(message).await.unwrap();
    
    let stats = transport.stats();
    assert_eq!(stats.packets_sent, 1);
    assert_eq!(stats.bytes_sent, 1000);
}

#[tokio::test]
async fn test_send_multiple_message_types() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    transport.start().await.unwrap();
    transport.connect("127.0.0.1:8080".parse().unwrap()).await.unwrap();
    
    // Send video frame
    transport.send(TransportMessage::VideoFrame {
        sequence: 1,
        timestamp: 1000,
        is_keyframe: true,
        data: Bytes::from(vec![0u8; 5000]),
    }).await.unwrap();
    
    // Send audio data
    transport.send(TransportMessage::AudioData {
        sequence: 1,
        timestamp: 1000,
        data: Bytes::from(vec![0u8; 200]),
    }).await.unwrap();
    
    // Send input event
    transport.send(TransportMessage::InputEvent {
        sequence: 1,
        event_data: Bytes::from(vec![0u8; 50]),
    }).await.unwrap();
    
    // Send control message
    transport.send(TransportMessage::Control {
        message_type: "resize".to_string(),
        payload: Bytes::from(vec![0u8; 20]),
    }).await.unwrap();
    
    // Send heartbeat
    transport.send(TransportMessage::Heartbeat {
        timestamp: 2000,
    }).await.unwrap();
    
    let stats = transport.stats();
    assert_eq!(stats.packets_sent, 5);
    assert_eq!(stats.bytes_sent, 5000 + 200 + 50 + 20 + 8);
}

#[tokio::test]
async fn test_receive_messages() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    transport.connect("127.0.0.1:8080".parse().unwrap()).await.unwrap();
    
    // Inject some messages
    transport.inject_message(TransportMessage::VideoFrame {
        sequence: 1,
        timestamp: 1000,
        is_keyframe: true,
        data: Bytes::from(vec![1u8; 1000]),
    }).await;
    
    transport.inject_message(TransportMessage::Heartbeat {
        timestamp: 2000,
    }).await;
    
    // Receive messages
    let msg1 = transport.receive().await.unwrap();
    match msg1 {
        TransportMessage::VideoFrame { sequence, .. } => assert_eq!(sequence, 1),
        _ => panic!("Expected VideoFrame"),
    }
    
    let msg2 = transport.receive().await.unwrap();
    match msg2 {
        TransportMessage::Heartbeat { timestamp } => assert_eq!(timestamp, 2000),
        _ => panic!("Expected Heartbeat"),
    }
    
    // No more messages
    assert!(transport.receive().await.is_err());
    
    let stats = transport.stats();
    assert_eq!(stats.packets_received, 2);
    assert_eq!(stats.bytes_received, 1000 + 8);
}

#[tokio::test]
async fn test_network_stats() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    transport.connect("127.0.0.1:8080".parse().unwrap()).await.unwrap();
    
    // Check RTT is set after connection
    let stats = transport.stats();
    assert_eq!(stats.rtt_ms, 5.0);
}

#[tokio::test]
async fn test_connection_state_transitions() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    // Test all state transitions
    assert_eq!(transport.state(), ConnectionState::Disconnected);
    
    transport.start().await.unwrap();
    assert_eq!(transport.state(), ConnectionState::Disconnected);
    
    // During connect, state should briefly be Connecting
    let connect_future = transport.connect("127.0.0.1:8080".parse().unwrap());
    // Note: In real implementation, we'd check Connecting state here
    connect_future.await.unwrap();
    assert_eq!(transport.state(), ConnectionState::Connected);
    
    transport.close().await.unwrap();
    assert_eq!(transport.state(), ConnectionState::Closed);
}

#[tokio::test]
async fn test_send_failure_after_n_messages() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config).fail_after(3);
    
    transport.connect("127.0.0.1:8080".parse().unwrap()).await.unwrap();
    
    // First 3 messages should succeed
    for i in 0..3 {
        transport.send(TransportMessage::Heartbeat { timestamp: i }).await.unwrap();
    }
    
    // 4th message should fail
    let result = transport.send(TransportMessage::Heartbeat { timestamp: 3 }).await;
    assert!(result.is_err());
    assert_eq!(transport.state(), ConnectionState::Failed);
}

#[tokio::test]
async fn test_accept_connection() {
    let config = TransportConfig::default();
    let mut transport = MockTransport::new(config);
    
    transport.start().await.unwrap();
    let peer_addr = transport.accept().await.unwrap();
    
    assert_eq!(peer_addr, "127.0.0.1:12345".parse::<SocketAddr>().unwrap());
    assert_eq!(transport.state(), ConnectionState::Connected);
}

#[test]
fn test_transport_protocol_equality() {
    assert_eq!(TransportProtocol::Quic, TransportProtocol::Quic);
    assert_ne!(TransportProtocol::Quic, TransportProtocol::WebRtcData);
}

#[test]
fn test_connection_state_equality() {
    assert_eq!(ConnectionState::Connected, ConnectionState::Connected);
    assert_ne!(ConnectionState::Connected, ConnectionState::Disconnected);
}

#[tokio::test]
async fn test_concurrent_send_receive() {
    use tokio::task;
    
    let config = TransportConfig::default();
    let transport = Arc::new(AsyncMutex::new(MockTransport::new(config)));
    
    transport.lock().await.connect("127.0.0.1:8080".parse().unwrap()).await.unwrap();
    
    // Inject messages for receiving
    for i in 0..5 {
        transport.lock().await.inject_message(TransportMessage::Heartbeat { timestamp: i }).await;
    }
    
    let transport_send = Arc::clone(&transport);
    let transport_recv = Arc::clone(&transport);
    
    // Spawn send task
    let send_task = task::spawn(async move {
        for i in 0..5 {
            transport_send.lock().await.send(TransportMessage::Heartbeat { timestamp: i }).await.unwrap();
            tokio::time::sleep(tokio::time::Duration::from_millis(10)).await;
        }
    });
    
    // Spawn receive task
    let recv_task = task::spawn(async move {
        let mut received = 0;
        while received < 5 {
            if transport_recv.lock().await.receive().await.is_ok() {
                received += 1;
            }
            tokio::time::sleep(tokio::time::Duration::from_millis(10)).await;
        }
        received
    });
    
    send_task.await.unwrap();
    let received_count = recv_task.await.unwrap();
    assert_eq!(received_count, 5);
}

#[test]
fn test_network_stats_default() {
    let stats = NetworkStats::default();
    assert_eq!(stats.bytes_sent, 0);
    assert_eq!(stats.bytes_received, 0);
    assert_eq!(stats.packets_sent, 0);
    assert_eq!(stats.packets_received, 0);
    assert_eq!(stats.packet_loss, 0.0);
    assert_eq!(stats.rtt_ms, 0.0);
    assert_eq!(stats.jitter_ms, 0.0);
    assert_eq!(stats.bandwidth_bps, 0);
}