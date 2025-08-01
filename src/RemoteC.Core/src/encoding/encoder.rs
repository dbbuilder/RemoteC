//! Frame encoder implementation

use super::*;
use super::error::EncodingError;
use std::sync::Arc;
use std::sync::Mutex;
use std::time::Instant;
use flate2::Compression;
use flate2::write::ZlibEncoder;
use std::io::Write;

/// Thread-safe frame encoder
pub struct FrameEncoder {
    config: Arc<Mutex<FrameEncodingConfig>>,
    stats: Arc<Mutex<EncoderStats>>,
}

// Export Result type for tests
pub use crate::Result;

/// Internal statistics for the encoder
#[derive(Debug, Default)]
struct EncoderStats {
    frames_encoded: u64,
    total_bytes_processed: u64,
    total_bytes_output: u64,
    total_encoding_time_us: u64,
}

impl FrameEncoder {
    /// Create a new frame encoder with the given configuration
    pub fn new(config: FrameEncodingConfig) -> crate::Result<Self> {
        // Validate configuration
        if config.quality > 100 {
            return Err(EncodingError::ConfigurationError(
                format!("Quality must be 0-100, got {}", config.quality)
            ).into());
        }
        
        if config.max_threads == 0 {
            return Err(EncodingError::ConfigurationError(
                "max_threads must be at least 1".to_string()
            ).into());
        }
        
        Ok(Self {
            config: Arc::new(Mutex::new(config)),
            stats: Arc::new(Mutex::new(EncoderStats::default())),
        })
    }
    
    /// Encode a BGRA frame
    pub fn encode_frame(&mut self, data: &[u8], width: u32, height: u32) -> crate::Result<EncodedFrame> {
        let start_time = Instant::now();
        
        // Validate dimensions
        if width == 0 || height == 0 || width > 8192 || height > 8192 {
            return Err(EncodingError::InvalidDimensions { width, height }.into());
        }
        
        // Validate data size (BGRA = 4 bytes per pixel)
        let expected_size = (width * height * 4) as usize;
        if data.len() != expected_size {
            return Err(EncodingError::InvalidFrameData {
                expected: expected_size,
                actual: data.len(),
            }.into());
        }
        
        // Get current configuration
        let config = self.config.lock().unwrap().clone();
        
        // Compress the frame based on format
        let compressed_data = match config.compression_format {
            CompressionFormat::None => {
                // No compression, just copy the data
                data.to_vec()
            }
            CompressionFormat::Zlib => {
                self.compress_zlib(data, config.quality)?
            }
            CompressionFormat::Lz4 => {
                // For now, we'll use a simple implementation
                // In production, we'd use the lz4 crate
                self.compress_lz4_simple(data)?
            }
            CompressionFormat::Zstd => {
                // For now, we'll use a simple implementation
                // In production, we'd use the zstd crate
                self.compress_zstd_simple(data, config.quality)?
            }
        };
        
        let encoding_duration_us = start_time.elapsed().as_micros() as u64;
        let compressed_size = compressed_data.len();
        let compression_ratio = data.len() as f32 / compressed_size as f32;
        
        // Update statistics
        {
            let mut stats = self.stats.lock().unwrap();
            stats.frames_encoded += 1;
            stats.total_bytes_processed += data.len() as u64;
            stats.total_bytes_output += compressed_size as u64;
            stats.total_encoding_time_us += encoding_duration_us;
        }
        
        // Create metadata
        let metadata = FrameMetadata {
            width,
            height,
            format: config.compression_format,
            original_size: data.len(),
            compressed_size,
            compression_ratio,
            timestamp: get_timestamp_ms(),
            encoding_duration_us,
        };
        
        Ok(EncodedFrame {
            data: compressed_data,
            metadata,
        })
    }
    
    /// Compress using zlib
    fn compress_zlib(&self, data: &[u8], quality: u8) -> crate::Result<Vec<u8>> {
        // Map quality (0-100) to zlib compression level (0-9)
        let compression_level = if quality >= 90 {
            Compression::best()
        } else if quality >= 70 {
            Compression::new(7)
        } else if quality >= 50 {
            Compression::new(5)
        } else if quality >= 30 {
            Compression::new(3)
        } else {
            Compression::fast()
        };
        
        let mut encoder = ZlibEncoder::new(Vec::new(), compression_level);
        encoder.write_all(data)
            .map_err(|e| crate::RemoteCError::from(EncodingError::CompressionFailed(format!("Zlib write failed: {}", e))))?;
        encoder.finish()
            .map_err(|e| crate::RemoteCError::from(EncodingError::CompressionFailed(format!("Zlib finish failed: {}", e))))
    }
    
    /// Simple LZ4 compression (placeholder)
    fn compress_lz4_simple(&self, data: &[u8]) -> crate::Result<Vec<u8>> {
        // For now, use a simple run-length encoding as placeholder
        // In production, use the lz4 crate
        let mut output = Vec::with_capacity(data.len() / 2);
        
        // Add a simple header to identify format
        output.extend_from_slice(b"LZ4\x00");
        
        // Simple compression: just remove consecutive duplicates
        let mut last_byte = None;
        let mut count = 0u8;
        
        for &byte in data {
            if Some(byte) == last_byte && count < 255 {
                count += 1;
            } else {
                if let Some(b) = last_byte {
                    output.push(count);
                    output.push(b);
                }
                last_byte = Some(byte);
                count = 1;
            }
        }
        
        if let Some(b) = last_byte {
            output.push(count);
            output.push(b);
        }
        
        Ok(output)
    }
    
    /// Simple Zstandard compression (placeholder)
    fn compress_zstd_simple(&self, data: &[u8], quality: u8) -> crate::Result<Vec<u8>> {
        // For now, use zlib with a different header as placeholder
        // In production, use the zstd crate
        let mut compressed = self.compress_zlib(data, quality)?;
        
        // Prepend a header to identify as "zstd"
        let mut output = Vec::with_capacity(compressed.len() + 4);
        output.extend_from_slice(b"ZSTD");
        output.append(&mut compressed);
        
        Ok(output)
    }
    
    /// Update encoder configuration
    pub fn update_config(&mut self, config: FrameEncodingConfig) -> crate::Result<()> {
        if config.quality > 100 {
            return Err(EncodingError::ConfigurationError(
                format!("Quality must be 0-100, got {}", config.quality)
            ).into());
        }
        
        if config.max_threads == 0 {
            return Err(EncodingError::ConfigurationError(
                "max_threads must be at least 1".to_string()
            ).into());
        }
        
        *self.config.lock().unwrap() = config;
        Ok(())
    }
    
    /// Get current configuration
    pub fn get_config(&self) -> FrameEncodingConfig {
        self.config.lock().unwrap().clone()
    }
    
    /// Get encoder statistics
    pub fn get_stats(&self) -> (u64, u64, u64, f64) {
        let stats = self.stats.lock().unwrap();
        let avg_encoding_time = if stats.frames_encoded > 0 {
            stats.total_encoding_time_us as f64 / stats.frames_encoded as f64
        } else {
            0.0
        };
        
        (
            stats.frames_encoded,
            stats.total_bytes_processed,
            stats.total_bytes_output,
            avg_encoding_time,
        )
    }
}

// Implement Send and Sync for thread safety
unsafe impl Send for FrameEncoder {}
unsafe impl Sync for FrameEncoder {}