//! Network transport module for RemoteC Core

use crate::Result;
use std::net::SocketAddr;

pub struct TransportConfig {
    pub protocol: TransportProtocol,
    pub port: u16,
    pub encryption: bool,
}

#[derive(Debug, Clone, Copy)]
pub enum TransportProtocol {
    WebRTC,
    UDP,
    TCP,
}

pub trait Transport: Send + Sync {
    fn connect(&mut self, addr: SocketAddr) -> Result<()>;
    fn disconnect(&mut self) -> Result<()>;
    fn send(&mut self, data: &[u8]) -> Result<()>;
    fn receive(&mut self) -> Result<Vec<u8>>;
}