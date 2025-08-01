//! Network transport module for RemoteC Core
//!
//! Provides high-performance network transport using QUIC and WebRTC protocols.

use crate::{Result, RemoteCError};
use bytes::Bytes;
use std::net::SocketAddr;
use std::time::Duration;

/// Transport protocol types
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TransportProtocol {
    /// QUIC protocol - reliable, low-latency
    Quic,
    /// WebRTC data channels - P2P capable
    WebRtcData,
    /// Raw UDP - lowest latency
    Udp,
}

/// Transport connection state
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ConnectionState {
    /// Not connected
    Disconnected,
    /// Attempting to connect
    Connecting,
    /// Successfully connected
    Connected,
    /// Connection failed
    Failed,
    /// Connection closed
    Closed,
}

/// Network statistics
#[derive(Debug, Default, Clone)]
pub struct NetworkStats {
    /// Bytes sent
    pub bytes_sent: u64,
    /// Bytes received
    pub bytes_received: u64,
    /// Packets sent
    pub packets_sent: u64,
    /// Packets received
    pub packets_received: u64,
    /// Packet loss percentage
    pub packet_loss: f32,
    /// Round-trip time in milliseconds
    pub rtt_ms: f32,
    /// Jitter in milliseconds
    pub jitter_ms: f32,
    /// Current bandwidth in bits per second
    pub bandwidth_bps: u64,
}

/// Transport configuration
#[derive(Debug, Clone)]
pub struct TransportConfig {
    /// Protocol to use
    pub protocol: TransportProtocol,
    /// Local bind address
    pub bind_addr: SocketAddr,
    /// Remote peer address (for client mode)
    pub remote_addr: Option<SocketAddr>,
    /// Maximum transmission unit
    pub mtu: usize,
    /// Connection timeout
    pub connect_timeout: Duration,
    /// Keep-alive interval
    pub keep_alive: Option<Duration>,
    /// Enable congestion control
    pub congestion_control: bool,
    /// Maximum retransmission attempts
    pub max_retries: u32,
}

impl Default for TransportConfig {
    fn default() -> Self {
        Self {
            protocol: TransportProtocol::Quic,
            bind_addr: "0.0.0.0:0".parse().unwrap(),
            remote_addr: None,
            mtu: 1200,
            connect_timeout: Duration::from_secs(10),
            keep_alive: Some(Duration::from_secs(30)),
            congestion_control: true,
            max_retries: 3,
        }
    }
}

/// Message types for the transport layer
#[derive(Debug, Clone)]
pub enum TransportMessage {
    /// Video frame data
    VideoFrame {
        sequence: u64,
        timestamp: u64,
        is_keyframe: bool,
        data: Bytes,
    },
    /// Audio data
    AudioData {
        sequence: u64,
        timestamp: u64,
        data: Bytes,
    },
    /// Input event
    InputEvent {
        sequence: u64,
        event_data: Bytes,
    },
    /// Control message
    Control {
        message_type: String,
        payload: Bytes,
    },
    /// Heartbeat/keepalive
    Heartbeat {
        timestamp: u64,
    },
}

/// Transport layer trait
#[async_trait::async_trait]
pub trait Transport: Send + Sync {
    /// Start the transport (bind/listen)
    async fn start(&mut self) -> Result<()>;
    
    /// Connect to a remote peer
    async fn connect(&mut self, addr: SocketAddr) -> Result<()>;
    
    /// Accept incoming connection
    async fn accept(&mut self) -> Result<SocketAddr>;
    
    /// Send a message
    async fn send(&mut self, message: TransportMessage) -> Result<()>;
    
    /// Receive a message
    async fn receive(&mut self) -> Result<TransportMessage>;
    
    /// Get current connection state
    fn state(&self) -> ConnectionState;
    
    /// Get network statistics
    fn stats(&self) -> NetworkStats;
    
    /// Close the connection
    async fn close(&mut self) -> Result<()>;
}

/// Transport event types
#[derive(Debug, Clone)]
pub enum TransportEvent {
    /// Connection established
    Connected(SocketAddr),
    /// Connection lost
    Disconnected(String),
    /// Error occurred
    Error(String),
    /// Statistics update
    StatsUpdate(NetworkStats),
}

/// Create a transport instance
pub async fn create_transport(config: TransportConfig) -> Result<Box<dyn Transport>> {
    match config.protocol {
        TransportProtocol::Quic => {
            Ok(Box::new(quic::QuicTransport::new(config).await?))
        }
        TransportProtocol::WebRtcData => {
            Err(RemoteCError::TransportError(
                "WebRTC transport not yet implemented".to_string()
            ))
        }
        TransportProtocol::Udp => {
            Err(RemoteCError::TransportError(
                "UDP transport not yet implemented".to_string()
            ))
        }
    }
}

#[cfg(test)]
mod tests;

// Protocol implementations
mod quic;
mod reliability;
mod congestion;