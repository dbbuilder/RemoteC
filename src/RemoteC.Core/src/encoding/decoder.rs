//! Frame decoder implementation

use super::*;
use std::time::Instant;
use flate2::read::ZlibDecoder;
use std::io::Read;

/// Configuration for frame decoder
#[derive(Debug, Clone)]
pub struct DecoderConfig {
    /// Maximum frame size in bytes to accept
    pub max_frame_size: usize,
    /// Enable additional validation checks
    pub enable_validation: bool,
    /// Number of threads for parallel decoding
    pub num_threads: usize,
}

impl Default for DecoderConfig {
    fn default() -> Self {
        Self {
            max_frame_size: 8192 * 8192 * 4, // 8K max
            enable_validation: true,
            num_threads: 1,
        }
    }
}

/// Decoded frame data
#[derive(Debug, Clone)]
pub struct DecodedFrame {
    /// Raw BGRA pixel data
    pub data: Vec<u8>,
    /// Frame width in pixels
    pub width: u32,
    /// Frame height in pixels
    pub height: u32,
    /// Compression format that was used
    pub format: CompressionFormat,
    /// Decoding duration in microseconds
    pub decoding_duration_us: u64,
}

/// Thread-safe frame decoder
pub struct FrameDecoder {
    config: DecoderConfig,
}

impl FrameDecoder {
    /// Create a new frame decoder with default configuration
    pub fn new() -> Self {
        Self {
            config: DecoderConfig::default(),
        }
    }
    
    /// Create a new frame decoder with custom configuration
    pub fn with_config(config: DecoderConfig) -> Self {
        Self { config }
    }
    
    /// Decode a single frame
    pub fn decode_frame(&self, encoded: &EncodedFrame) -> crate::Result<DecodedFrame> {
        let start_time = Instant::now();
        
        // Validate frame size if enabled
        if self.config.enable_validation {
            let expected_size = (encoded.metadata.width * encoded.metadata.height * 4) as usize;
            if expected_size > self.config.max_frame_size {
                return Err(EncodingError::InvalidFrameData {
                    expected: self.config.max_frame_size,
                    actual: expected_size,
                }.into());
            }
        }
        
        // Decode based on format
        let decoded_data = match encoded.metadata.format {
            CompressionFormat::None => {
                // No decompression needed
                encoded.data.clone()
            }
            CompressionFormat::Zlib => {
                self.decompress_zlib(&encoded.data, encoded.metadata.original_size)?
            }
            CompressionFormat::Lz4 => {
                self.decompress_lz4_simple(&encoded.data)?
            }
            CompressionFormat::Zstd => {
                self.decompress_zstd_simple(&encoded.data)?
            }
        };
        
        // Validate decompressed size
        let expected_size = (encoded.metadata.width * encoded.metadata.height * 4) as usize;
        if decoded_data.len() != expected_size {
            return Err(EncodingError::InvalidFrameData {
                expected: expected_size,
                actual: decoded_data.len(),
            }.into());
        }
        
        let decoding_duration_us = start_time.elapsed().as_micros() as u64;
        
        Ok(DecodedFrame {
            data: decoded_data,
            width: encoded.metadata.width,
            height: encoded.metadata.height,
            format: encoded.metadata.format,
            decoding_duration_us,
        })
    }
    
    /// Decode multiple frames in batch
    pub fn decode_batch(&self, frames: &[EncodedFrame]) -> Vec<crate::Result<DecodedFrame>> {
        frames.iter()
            .map(|frame| self.decode_frame(frame))
            .collect()
    }
    
    /// Decompress using zlib
    fn decompress_zlib(&self, data: &[u8], expected_size: usize) -> crate::Result<Vec<u8>> {
        let mut decoder = ZlibDecoder::new(data);
        let mut decompressed = Vec::with_capacity(expected_size);
        
        match decoder.read_to_end(&mut decompressed) {
            Ok(_) => Ok(decompressed),
            Err(e) => Err(EncodingError::CompressionFailed(
                format!("Zlib decompression failed: {}", e)
            ).into()),
        }
    }
    
    /// Simple LZ4 decompression (placeholder)
    fn decompress_lz4_simple(&self, data: &[u8]) -> crate::Result<Vec<u8>> {
        // Check for our simple header
        if data.len() < 4 || &data[0..4] != b"LZ4\x00" {
            return Err(EncodingError::CompressionFailed(
                "Invalid LZ4 data".to_string()
            ).into());
        }
        
        // Simple decompression: reverse our simple RLE
        let mut output = Vec::new();
        let mut i = 4; // Skip header
        
        while i + 1 < data.len() {
            let count = data[i];
            let byte = data[i + 1];
            for _ in 0..count {
                output.push(byte);
            }
            i += 2;
        }
        
        Ok(output)
    }
    
    /// Simple Zstandard decompression (placeholder)
    fn decompress_zstd_simple(&self, data: &[u8]) -> crate::Result<Vec<u8>> {
        // Check for our header
        if data.len() < 4 || &data[0..4] != b"ZSTD" {
            return Err(EncodingError::CompressionFailed(
                "Invalid ZSTD data".to_string()
            ).into());
        }
        
        // For now, it's just zlib with a different header
        self.decompress_zlib(&data[4..], 0)
    }
}

// Implement Send and Sync for thread safety
unsafe impl Send for FrameDecoder {}
unsafe impl Sync for FrameDecoder {}

impl Default for FrameDecoder {
    fn default() -> Self {
        Self::new()
    }
}

/// Decoding-specific errors
#[derive(Debug)]
pub enum DecodingError {
    /// Invalid compressed data
    InvalidData(String),
    /// Decompression failed
    DecompressionFailed(String),
    /// Size validation failed
    SizeValidationFailed { expected: usize, actual: usize },
    /// Format not supported
    UnsupportedFormat(CompressionFormat),
}

impl std::fmt::Display for DecodingError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            DecodingError::InvalidData(msg) => write!(f, "Invalid data: {}", msg),
            DecodingError::DecompressionFailed(msg) => write!(f, "Decompression failed: {}", msg),
            DecodingError::SizeValidationFailed { expected, actual } => {
                write!(f, "Size validation failed: expected {} bytes, got {} bytes", expected, actual)
            }
            DecodingError::UnsupportedFormat(format) => {
                write!(f, "Unsupported format: {:?}", format)
            }
        }
    }
}

impl std::error::Error for DecodingError {}