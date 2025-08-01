//! Frame encoding and compression module
//! 
//! This module provides high-performance frame encoding capabilities
//! for the RemoteC remote control system.

use std::time::{SystemTime, UNIX_EPOCH};

pub use self::encoder::*;
pub use self::decoder::*;
pub use self::error::*;

mod types;
mod encoder;
mod decoder;
mod error;

/// Compression format for encoded frames
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
#[repr(C)]
pub enum CompressionFormat {
    /// No compression (raw BGRA data)
    None = 0,
    /// Zlib compression (deflate)
    Zlib = 1,
    /// LZ4 compression (fast)
    Lz4 = 2,
    /// Zstandard compression (balanced)
    Zstd = 3,
}

/// Configuration for frame encoding
#[derive(Debug, Clone)]
pub struct FrameEncodingConfig {
    /// Compression format to use
    pub compression_format: CompressionFormat,
    /// Quality level (0-100, higher = better quality/less compression)
    pub quality: u8,
    /// Maximum threads for parallel encoding
    pub max_threads: usize,
}

impl Default for FrameEncodingConfig {
    fn default() -> Self {
        Self {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 4,
        }
    }
}

/// Metadata about an encoded frame
#[derive(Debug, Clone)]
pub struct FrameMetadata {
    /// Frame width in pixels
    pub width: u32,
    /// Frame height in pixels
    pub height: u32,
    /// Compression format used
    pub format: CompressionFormat,
    /// Original size in bytes (before compression)
    pub original_size: usize,
    /// Compressed size in bytes
    pub compressed_size: usize,
    /// Compression ratio (original/compressed)
    pub compression_ratio: f32,
    /// Timestamp when frame was encoded (milliseconds since UNIX epoch)
    pub timestamp: u64,
    /// Encoding duration in microseconds
    pub encoding_duration_us: u64,
}

/// An encoded frame with its data and metadata
#[derive(Debug, Clone)]
pub struct EncodedFrame {
    /// Compressed frame data
    pub data: Vec<u8>,
    /// Frame metadata
    pub metadata: FrameMetadata,
}

/// Get current timestamp in milliseconds since UNIX epoch
fn get_timestamp_ms() -> u64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_millis() as u64
}