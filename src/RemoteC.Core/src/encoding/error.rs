//! Error types for frame encoding

use std::error::Error;
use std::fmt;

/// Frame encoding errors
#[derive(Debug)]
pub enum EncodingError {
    /// Invalid frame dimensions
    InvalidDimensions { width: u32, height: u32 },
    /// Invalid frame data (wrong size for dimensions)
    InvalidFrameData { expected: usize, actual: usize },
    /// Compression failed
    CompressionFailed(String),
    /// Unsupported compression format
    UnsupportedFormat(super::CompressionFormat),
    /// Configuration error
    ConfigurationError(String),
    /// Thread pool error
    ThreadPoolError(String),
}

impl fmt::Display for EncodingError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            EncodingError::InvalidDimensions { width, height } => {
                write!(f, "Invalid frame dimensions: {}x{}", width, height)
            }
            EncodingError::InvalidFrameData { expected, actual } => {
                write!(f, "Invalid frame data size: expected {} bytes, got {} bytes", expected, actual)
            }
            EncodingError::CompressionFailed(msg) => {
                write!(f, "Compression failed: {}", msg)
            }
            EncodingError::UnsupportedFormat(format) => {
                write!(f, "Unsupported compression format: {:?}", format)
            }
            EncodingError::ConfigurationError(msg) => {
                write!(f, "Configuration error: {}", msg)
            }
            EncodingError::ThreadPoolError(msg) => {
                write!(f, "Thread pool error: {}", msg)
            }
        }
    }
}

impl Error for EncodingError {}

/// Result type for encoding operations
pub type EncodingResult<T> = Result<T, EncodingError>;