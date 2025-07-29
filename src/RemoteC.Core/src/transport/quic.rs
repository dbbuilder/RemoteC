//! QUIC transport implementation using Quinn

use super::{Transport, TransportConfig, TransportMessage, ConnectionState, NetworkStats};
use crate::{Result, RemoteCError};
use bytes::Bytes;
use quinn::{Endpoint, Connection, RecvStream, SendStream};
use std::net::SocketAddr;
use std::sync::Arc;
use tokio::sync::Mutex;

/// QUIC transport implementation
pub struct QuicTransport {
    config: TransportConfig,
    endpoint: Option<Endpoint>,
    connection: Option<Connection>,
    state: ConnectionState,
    stats: Arc<Mutex<NetworkStats>>,
}

impl QuicTransport {
    pub async fn new(config: TransportConfig) -> Result<Self> {
        Ok(Self {
            config,
            endpoint: None,
            connection: None,
            state: ConnectionState::Disconnected,
            stats: Arc::new(Mutex::new(NetworkStats::default())),
        })
    }
    
    fn create_quinn_config() -> quinn::ServerConfig {
        // Generate self-signed certificate for now
        let cert = rcgen::generate_simple_self_signed(vec!["localhost".to_string()])
            .expect("Failed to generate certificate");
        let cert_der = cert.serialize_der().expect("Failed to serialize certificate");
        let priv_key = cert.serialize_private_key_der();
        let priv_key = rustls::PrivateKey(priv_key);
        let cert_chain = vec![rustls::Certificate(cert_der)];
        
        let mut server_config = quinn::ServerConfig::with_single_cert(cert_chain, priv_key)
            .expect("Failed to create server config");
        
        let transport_config = Arc::get_mut(&mut server_config.transport)
            .expect("Failed to get transport config");
        
        // Configure for low latency
        transport_config.max_idle_timeout(Some(std::time::Duration::from_secs(60).try_into().unwrap()));
        transport_config.keep_alive_interval(Some(std::time::Duration::from_secs(10)));
        
        server_config
    }
    
    fn create_client_config() -> quinn::ClientConfig {
        let crypto = rustls::ClientConfig::builder()
            .with_safe_defaults()
            .with_custom_certificate_verifier(SkipServerVerification::new())
            .with_no_client_auth();
        
        quinn::ClientConfig::new(Arc::new(crypto))
    }
}

#[async_trait::async_trait]
impl Transport for QuicTransport {
    async fn start(&mut self) -> Result<()> {
        let server_config = Self::create_quinn_config();
        
        let endpoint = Endpoint::server(server_config, self.config.bind_addr)
            .map_err(|e| RemoteCError::TransportError(format!("Failed to bind: {}", e)))?;
        
        self.endpoint = Some(endpoint);
        self.state = ConnectionState::Disconnected;
        Ok(())
    }
    
    async fn connect(&mut self, addr: SocketAddr) -> Result<()> {
        self.state = ConnectionState::Connecting;
        
        let client_config = Self::create_client_config();
        
        let mut endpoint = Endpoint::client("0.0.0.0:0".parse().unwrap())
            .map_err(|e| RemoteCError::TransportError(format!("Failed to create client: {}", e)))?;
        
        endpoint.set_default_client_config(client_config);
        
        let connection = endpoint
            .connect(addr, "localhost")
            .map_err(|e| RemoteCError::TransportError(format!("Connection failed: {}", e)))?
            .await
            .map_err(|e| RemoteCError::TransportError(format!("Connection failed: {}", e)))?;
        
        self.endpoint = Some(endpoint);
        self.connection = Some(connection);
        self.state = ConnectionState::Connected;
        
        // Update RTT from connection stats
        if let Some(conn) = &self.connection {
            let stats = conn.stats();
            let mut net_stats = self.stats.lock().await;
            net_stats.rtt_ms = stats.path.rtt.as_millis() as f32;
        }
        
        Ok(())
    }
    
    async fn accept(&mut self) -> Result<SocketAddr> {
        let endpoint = self.endpoint.as_ref()
            .ok_or_else(|| RemoteCError::TransportError("Transport not started".to_string()))?;
        
        let connecting = endpoint.accept().await
            .ok_or_else(|| RemoteCError::TransportError("No incoming connections".to_string()))?;
        
        let connection = connecting.await
            .map_err(|e| RemoteCError::TransportError(format!("Accept failed: {}", e)))?;
        
        let remote_addr = connection.remote_address();
        self.connection = Some(connection);
        self.state = ConnectionState::Connected;
        
        Ok(remote_addr)
    }
    
    async fn send(&mut self, message: TransportMessage) -> Result<()> {
        let connection = self.connection.as_ref()
            .ok_or_else(|| RemoteCError::TransportError("Not connected".to_string()))?;
        
        // Serialize message
        let data = serialize_message(&message)?;
        
        // Open a new stream for this message
        let mut stream = connection.open_uni().await
            .map_err(|e| RemoteCError::TransportError(format!("Failed to open stream: {}", e)))?;
        
        // Send the data
        stream.write_all(&data).await
            .map_err(|e| RemoteCError::TransportError(format!("Send failed: {}", e)))?;
        
        stream.finish().await
            .map_err(|e| RemoteCError::TransportError(format!("Stream finish failed: {}", e)))?;
        
        // Update stats
        let mut stats = self.stats.lock().await;
        stats.bytes_sent += data.len() as u64;
        stats.packets_sent += 1;
        
        Ok(())
    }
    
    async fn receive(&mut self) -> Result<TransportMessage> {
        let connection = self.connection.as_ref()
            .ok_or_else(|| RemoteCError::TransportError("Not connected".to_string()))?;
        
        // Accept incoming stream
        let mut stream = connection.accept_uni().await
            .map_err(|e| RemoteCError::TransportError(format!("Failed to accept stream: {}", e)))?;
        
        // Read all data from stream
        let data = stream.read_to_end(self.config.mtu * 10).await
            .map_err(|e| RemoteCError::TransportError(format!("Receive failed: {}", e)))?;
        
        // Update stats
        let mut stats = self.stats.lock().await;
        stats.bytes_received += data.len() as u64;
        stats.packets_received += 1;
        
        // Update RTT from connection
        let conn_stats = connection.stats();
        stats.rtt_ms = conn_stats.path.rtt.as_millis() as f32;
        
        // Deserialize message
        deserialize_message(&data)
    }
    
    fn state(&self) -> ConnectionState {
        self.state
    }
    
    fn stats(&self) -> NetworkStats {
        futures::executor::block_on(async {
            self.stats.lock().await.clone()
        })
    }
    
    async fn close(&mut self) -> Result<()> {
        if let Some(connection) = self.connection.take() {
            connection.close(0u32.into(), b"closing");
        }
        
        if let Some(endpoint) = self.endpoint.take() {
            endpoint.close(0u32.into(), b"shutdown");
        }
        
        self.state = ConnectionState::Closed;
        Ok(())
    }
}

/// Helper to serialize transport messages
fn serialize_message(message: &TransportMessage) -> Result<Vec<u8>> {
    // Simple serialization - in production, use bincode or similar
    use std::io::Write;
    let mut buffer = Vec::new();
    
    match message {
        TransportMessage::VideoFrame { sequence, timestamp, is_keyframe, data } => {
            buffer.write_all(&[0u8])?; // Message type
            buffer.write_all(&sequence.to_le_bytes())?;
            buffer.write_all(&timestamp.to_le_bytes())?;
            buffer.write_all(&[*is_keyframe as u8])?;
            buffer.write_all(&(data.len() as u32).to_le_bytes())?;
            buffer.write_all(data)?;
        }
        TransportMessage::AudioData { sequence, timestamp, data } => {
            buffer.write_all(&[1u8])?; // Message type
            buffer.write_all(&sequence.to_le_bytes())?;
            buffer.write_all(&timestamp.to_le_bytes())?;
            buffer.write_all(&(data.len() as u32).to_le_bytes())?;
            buffer.write_all(data)?;
        }
        TransportMessage::InputEvent { sequence, event_data } => {
            buffer.write_all(&[2u8])?; // Message type
            buffer.write_all(&sequence.to_le_bytes())?;
            buffer.write_all(&(event_data.len() as u32).to_le_bytes())?;
            buffer.write_all(event_data)?;
        }
        TransportMessage::Control { message_type, payload } => {
            buffer.write_all(&[3u8])?; // Message type
            let type_bytes = message_type.as_bytes();
            buffer.write_all(&(type_bytes.len() as u32).to_le_bytes())?;
            buffer.write_all(type_bytes)?;
            buffer.write_all(&(payload.len() as u32).to_le_bytes())?;
            buffer.write_all(payload)?;
        }
        TransportMessage::Heartbeat { timestamp } => {
            buffer.write_all(&[4u8])?; // Message type
            buffer.write_all(&timestamp.to_le_bytes())?;
        }
    }
    
    Ok(buffer)
}

/// Helper to deserialize transport messages
fn deserialize_message(data: &[u8]) -> Result<TransportMessage> {
    use std::io::Read;
    let mut cursor = std::io::Cursor::new(data);
    
    let mut msg_type = [0u8];
    cursor.read_exact(&mut msg_type)?;
    
    match msg_type[0] {
        0 => { // VideoFrame
            let mut sequence_bytes = [0u8; 8];
            cursor.read_exact(&mut sequence_bytes)?;
            let sequence = u64::from_le_bytes(sequence_bytes);
            
            let mut timestamp_bytes = [0u8; 8];
            cursor.read_exact(&mut timestamp_bytes)?;
            let timestamp = u64::from_le_bytes(timestamp_bytes);
            
            let mut is_keyframe_byte = [0u8];
            cursor.read_exact(&mut is_keyframe_byte)?;
            let is_keyframe = is_keyframe_byte[0] != 0;
            
            let mut len_bytes = [0u8; 4];
            cursor.read_exact(&mut len_bytes)?;
            let len = u32::from_le_bytes(len_bytes) as usize;
            
            let mut data = vec![0u8; len];
            cursor.read_exact(&mut data)?;
            
            Ok(TransportMessage::VideoFrame {
                sequence,
                timestamp,
                is_keyframe,
                data: Bytes::from(data),
            })
        }
        1 => { // AudioData
            let mut sequence_bytes = [0u8; 8];
            cursor.read_exact(&mut sequence_bytes)?;
            let sequence = u64::from_le_bytes(sequence_bytes);
            
            let mut timestamp_bytes = [0u8; 8];
            cursor.read_exact(&mut timestamp_bytes)?;
            let timestamp = u64::from_le_bytes(timestamp_bytes);
            
            let mut len_bytes = [0u8; 4];
            cursor.read_exact(&mut len_bytes)?;
            let len = u32::from_le_bytes(len_bytes) as usize;
            
            let mut data = vec![0u8; len];
            cursor.read_exact(&mut data)?;
            
            Ok(TransportMessage::AudioData {
                sequence,
                timestamp,
                data: Bytes::from(data),
            })
        }
        2 => { // InputEvent
            let mut sequence_bytes = [0u8; 8];
            cursor.read_exact(&mut sequence_bytes)?;
            let sequence = u64::from_le_bytes(sequence_bytes);
            
            let mut len_bytes = [0u8; 4];
            cursor.read_exact(&mut len_bytes)?;
            let len = u32::from_le_bytes(len_bytes) as usize;
            
            let mut event_data = vec![0u8; len];
            cursor.read_exact(&mut event_data)?;
            
            Ok(TransportMessage::InputEvent {
                sequence,
                event_data: Bytes::from(event_data),
            })
        }
        3 => { // Control
            let mut type_len_bytes = [0u8; 4];
            cursor.read_exact(&mut type_len_bytes)?;
            let type_len = u32::from_le_bytes(type_len_bytes) as usize;
            
            let mut type_bytes = vec![0u8; type_len];
            cursor.read_exact(&mut type_bytes)?;
            let message_type = String::from_utf8(type_bytes)
                .map_err(|_| RemoteCError::TransportError("Invalid UTF-8 in message type".to_string()))?;
            
            let mut payload_len_bytes = [0u8; 4];
            cursor.read_exact(&mut payload_len_bytes)?;
            let payload_len = u32::from_le_bytes(payload_len_bytes) as usize;
            
            let mut payload = vec![0u8; payload_len];
            cursor.read_exact(&mut payload)?;
            
            Ok(TransportMessage::Control {
                message_type,
                payload: Bytes::from(payload),
            })
        }
        4 => { // Heartbeat
            let mut timestamp_bytes = [0u8; 8];
            cursor.read_exact(&mut timestamp_bytes)?;
            let timestamp = u64::from_le_bytes(timestamp_bytes);
            
            Ok(TransportMessage::Heartbeat { timestamp })
        }
        _ => Err(RemoteCError::TransportError(format!("Unknown message type: {}", msg_type[0]))),
    }
}

/// Certificate verifier that accepts any certificate (for development)
struct SkipServerVerification;

impl SkipServerVerification {
    fn new() -> Arc<Self> {
        Arc::new(Self)
    }
}

impl rustls::client::ServerCertVerifier for SkipServerVerification {
    fn verify_server_cert(
        &self,
        _end_entity: &rustls::Certificate,
        _intermediates: &[rustls::Certificate],
        _server_name: &rustls::ServerName,
        _scts: &mut dyn Iterator<Item = &[u8]>,
        _ocsp_response: &[u8],
        _now: std::time::SystemTime,
    ) -> std::result::Result<rustls::client::ServerCertVerified, rustls::Error> {
        Ok(rustls::client::ServerCertVerified::assertion())
    }
}