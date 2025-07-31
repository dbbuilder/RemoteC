//! RemoteC Core - High-performance remote control engine
//! 
//! This crate provides the core functionality for RemoteC's remote control
//! capabilities, including screen capture, input simulation, video encoding,
//! and network transport.

#![warn(missing_docs)]
#![warn(clippy::all)]
#![warn(clippy::pedantic)]
#![allow(clippy::module_name_repetitions)]

pub mod capture;
pub mod encoding;
pub mod ffi;
pub mod input;
pub mod logging;
pub mod transport;
pub mod video;

use thiserror::Error;

/// Result type for RemoteC Core operations
pub type Result<T> = std::result::Result<T, RemoteCError>;

/// Errors that can occur in RemoteC Core
#[derive(Error, Debug)]
pub enum RemoteCError {
    /// IO error
    #[error("IO error: {0}")]
    IoError(#[from] std::io::Error),
    /// Screen capture error
    #[error("Screen capture error: {0}")]
    CaptureError(String),
    
    /// Input simulation error
    #[error("Input simulation error: {0}")]
    InputError(String),
    
    /// Video encoding error
    #[error("Video encoding error: {0}")]
    EncodingError(String),
    
    /// Video decoding error
    #[error("Video decoding error: {0}")]
    DecodingError(String),
    
    /// Network transport error
    #[error("Network transport error: {0}")]
    TransportError(String),
    
    /// FFI error
    #[error("FFI error: {0}")]
    FfiError(String),
    
    /// Platform not supported
    #[error("Platform not supported: {0}")]
    UnsupportedPlatform(String),
    
    /// Initialization failed
    #[error("Initialization failed: {0}")]
    InitializationFailed(String),
    
    /// Generic error
    #[error("Error: {0}")]
    Other(String),
}

/// Initialize the RemoteC Core library
pub fn initialize() -> Result<()> {
    logging::init_logging();
    log::info!("RemoteC Core initialized");
    Ok(())
}

/// Get the version of RemoteC Core
pub fn version() -> &'static str {
    env!("CARGO_PKG_VERSION")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_version() {
        let version = version();
        assert!(!version.is_empty());
    }

    #[test]
    fn test_initialize() {
        let result = initialize();
        assert!(result.is_ok());
    }
}