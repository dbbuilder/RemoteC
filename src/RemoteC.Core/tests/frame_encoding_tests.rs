//! Comprehensive test suite for frame encoding/compression
//! 
//! This test suite follows Test-Driven Development (TDD) methodology.
//! These tests define the expected behavior BEFORE implementation.
//! All tests should initially FAIL (RED phase) until implementation is complete.

use std::time::{Duration, Instant};
use criterion::{black_box, Criterion};

// Note: These imports will fail until the frame encoding module is implemented
// This is expected as part of the TDD RED phase
use crate::encoding::{
    FrameEncoder, FrameEncodingConfig, CompressionFormat, EncodedFrame, 
    FrameMetadata, EncodingError
};
use crate::Result;

/// Test fixture for frame encoding tests
struct FrameEncodingTestFixture {
    encoder: FrameEncoder,
    test_frame_1920x1080: Vec<u8>,
    test_frame_1280x720: Vec<u8>,
    test_frame_640x480: Vec<u8>,
}

impl FrameEncodingTestFixture {
    fn new() -> Self {
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 4,
        };
        
        Self {
            encoder: FrameEncoder::new(config).expect("Failed to create encoder"),
            test_frame_1920x1080: create_test_bgra_frame(1920, 1080),
            test_frame_1280x720: create_test_bgra_frame(1280, 720),
            test_frame_640x480: create_test_bgra_frame(640, 480),
        }
    }
}

/// Create a test BGRA frame with predictable pattern
fn create_test_bgra_frame(width: u32, height: u32) -> Vec<u8> {
    let mut frame = Vec::with_capacity((width * height * 4) as usize);
    
    for y in 0..height {
        for x in 0..width {
            // Create a gradient pattern for testing
            let r = ((x * 255) / width) as u8;
            let g = ((y * 255) / height) as u8;
            let b = ((x + y) % 256) as u8;
            let a = 255u8; // Full alpha
            
            // BGRA format
            frame.push(b);
            frame.push(g);
            frame.push(r);
            frame.push(a);
        }
    }
    
    frame
}

/// Create corrupted/invalid frame data for error testing
fn create_invalid_frame_data() -> Vec<u8> {
    vec![0xFF; 100] // Too small for any meaningful resolution
}

#[cfg(test)]
mod frame_encoding_tests {
    use super::*;
    use std::sync::Arc;
    use std::thread;

    /// Test basic BGRA frame encoding functionality
    #[test]
    fn test_basic_bgra_frame_encoding() {
        let mut fixture = FrameEncodingTestFixture::new();
        
        let result = fixture.encoder.encode_frame(
            &fixture.test_frame_1920x1080,
            1920,
            1080,
        );
        
        assert!(result.is_ok(), "Basic BGRA frame encoding should succeed");
        
        let encoded_frame = result.unwrap();
        assert!(!encoded_frame.data.is_empty(), "Encoded frame should contain data");
        assert_eq!(encoded_frame.metadata.width, 1920, "Width should be preserved");
        assert_eq!(encoded_frame.metadata.height, 1080, "Height should be preserved");
        assert_eq!(encoded_frame.metadata.format, CompressionFormat::Zlib, "Format should match config");
        assert!(encoded_frame.metadata.original_size > 0, "Original size should be recorded");
        assert!(encoded_frame.metadata.compressed_size > 0, "Compressed size should be recorded");
        assert!(encoded_frame.metadata.compression_ratio > 0.0, "Compression ratio should be calculated");
        
        // Verify timestamp is recent (within last second)
        let now = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_millis() as u64;
        assert!(
            encoded_frame.metadata.timestamp <= now && 
            encoded_frame.metadata.timestamp > now - 1000,
            "Timestamp should be recent"
        );
    }

    /// Test frame encoding with different resolutions
    #[test]
    fn test_frame_encoding_different_resolutions() {
        let mut fixture = FrameEncodingTestFixture::new();
        
        // Test 1920x1080
        let result_1080p = fixture.encoder.encode_frame(&fixture.test_frame_1920x1080, 1920, 1080);
        assert!(result_1080p.is_ok(), "1920x1080 encoding should succeed");
        
        // Test 1280x720
        let result_720p = fixture.encoder.encode_frame(&fixture.test_frame_1280x720, 1280, 720);
        assert!(result_720p.is_ok(), "1280x720 encoding should succeed");
        
        // Test 640x480
        let result_480p = fixture.encoder.encode_frame(&fixture.test_frame_640x480, 640, 480);
        assert!(result_480p.is_ok(), "640x480 encoding should succeed");
        
        // Verify different compression sizes
        let encoded_1080p = result_1080p.unwrap();
        let encoded_720p = result_720p.unwrap();
        let encoded_480p = result_480p.unwrap();
        
        assert!(
            encoded_1080p.data.len() > encoded_720p.data.len(),
            "1080p encoded frame should be larger than 720p"
        );
        assert!(
            encoded_720p.data.len() > encoded_480p.data.len(),
            "720p encoded frame should be larger than 480p"
        );
    }

    /// Test configurable compression quality settings
    #[test]
    fn test_configurable_compression_quality() {
        let low_quality_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 20, // Low quality = higher compression
            max_threads: 1,
        };
        
        let high_quality_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 90, // High quality = lower compression
            max_threads: 1,
        };
        
        let mut low_encoder = FrameEncoder::new(low_quality_config)
            .expect("Failed to create low quality encoder");
        let mut high_encoder = FrameEncoder::new(high_quality_config)
            .expect("Failed to create high quality encoder");
        
        let test_frame = create_test_bgra_frame(1280, 720);
        
        let low_result = low_encoder.encode_frame(&test_frame, 1280, 720);
        let high_result = high_encoder.encode_frame(&test_frame, 1280, 720);
        
        assert!(low_result.is_ok(), "Low quality encoding should succeed");
        assert!(high_result.is_ok(), "High quality encoding should succeed");
        
        let low_encoded = low_result.unwrap();
        let high_encoded = high_result.unwrap();
        
        assert!(
            low_encoded.data.len() <= high_encoded.data.len(),
            "Low quality should produce smaller or equal file size"
        );
        assert!(
            low_encoded.metadata.compression_ratio >= high_encoded.metadata.compression_ratio,
            "Low quality should have better compression ratio"
        );
    }

    /// Test different compression formats
    #[test]
    fn test_different_compression_formats() {
        let zlib_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let lz4_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Lz4,
            quality: 80,
            max_threads: 1,
        };
        
        let zstd_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zstd,
            quality: 80,
            max_threads: 1,
        };
        
        let mut zlib_encoder = FrameEncoder::new(zlib_config)
            .expect("Failed to create zlib encoder");
        let mut lz4_encoder = FrameEncoder::new(lz4_config)
            .expect("Failed to create lz4 encoder");
        let mut zstd_encoder = FrameEncoder::new(zstd_config)
            .expect("Failed to create zstd encoder");
        
        let test_frame = create_test_bgra_frame(1920, 1080);
        
        let zlib_result = zlib_encoder.encode_frame(&test_frame, 1920, 1080);
        let lz4_result = lz4_encoder.encode_frame(&test_frame, 1920, 1080);
        let zstd_result = zstd_encoder.encode_frame(&test_frame, 1920, 1080);
        
        assert!(zlib_result.is_ok(), "Zlib encoding should succeed");
        assert!(lz4_result.is_ok(), "LZ4 encoding should succeed");
        assert!(zstd_result.is_ok(), "Zstd encoding should succeed");
        
        let zlib_encoded = zlib_result.unwrap();
        let lz4_encoded = lz4_result.unwrap();
        let zstd_encoded = zstd_result.unwrap();
        
        assert_eq!(zlib_encoded.metadata.format, CompressionFormat::Zlib);
        assert_eq!(lz4_encoded.metadata.format, CompressionFormat::Lz4);
        assert_eq!(zstd_encoded.metadata.format, CompressionFormat::Zstd);
        
        // All should produce valid compressed data
        assert!(!zlib_encoded.data.is_empty());
        assert!(!lz4_encoded.data.is_empty());
        assert!(!zstd_encoded.data.is_empty());
    }

    /// Test error handling for invalid input data
    #[test]
    fn test_error_handling_invalid_input() {
        let mut fixture = FrameEncodingTestFixture::new();
        
        // Test with empty frame data
        let empty_result = fixture.encoder.encode_frame(&[], 1920, 1080);
        assert!(empty_result.is_err(), "Empty frame data should fail");
        assert!(matches!(
            empty_result.unwrap_err(),
            crate::RemoteCError::EncodingError(_)
        ));
        
        // Test with insufficient frame data
        let insufficient_data = vec![0u8; 100]; // Way too small for 1920x1080
        let insufficient_result = fixture.encoder.encode_frame(&insufficient_data, 1920, 1080);
        assert!(insufficient_result.is_err(), "Insufficient frame data should fail");
        
        // Test with zero dimensions
        let zero_width_result = fixture.encoder.encode_frame(&fixture.test_frame_640x480, 0, 480);
        assert!(zero_width_result.is_err(), "Zero width should fail");
        
        let zero_height_result = fixture.encoder.encode_frame(&fixture.test_frame_640x480, 640, 0);
        assert!(zero_height_result.is_err(), "Zero height should fail");
        
        // Test with mismatched dimensions
        let mismatched_result = fixture.encoder.encode_frame(&fixture.test_frame_640x480, 1920, 1080);
        assert!(mismatched_result.is_err(), "Mismatched dimensions should fail");
    }

    /// Test performance requirement: encoding time < 50ms for 1920x1080
    #[test]
    fn test_performance_1920x1080_under_50ms() {
        let mut fixture = FrameEncodingTestFixture::new();
        
        // Warm up
        for _ in 0..3 {
            let _ = fixture.encoder.encode_frame(&fixture.test_frame_1920x1080, 1920, 1080);
        }
        
        // Measure performance over multiple iterations
        let iterations = 10;
        let mut total_duration = Duration::new(0, 0);
        
        for _ in 0..iterations {
            let start = Instant::now();
            let result = fixture.encoder.encode_frame(&fixture.test_frame_1920x1080, 1920, 1080);
            let duration = start.elapsed();
            
            assert!(result.is_ok(), "Encoding should succeed during performance test");
            total_duration += duration;
        }
        
        let average_duration = total_duration / iterations;
        assert!(
            average_duration < Duration::from_millis(50),
            "Average encoding time {} ms should be less than 50ms",
            average_duration.as_millis()
        );
        
        println!(
            "Performance test passed: Average encoding time for 1920x1080: {} ms",
            average_duration.as_millis()
        );
    }

    /// Test memory efficiency and proper cleanup
    #[test]
    fn test_memory_efficiency_and_cleanup() {
        // Test multiple encoders to ensure no global state pollution
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 2,
        };
        
        let test_frame = create_test_bgra_frame(1280, 720);
        
        // Create and drop multiple encoders
        for i in 0..10 {
            let mut encoder = FrameEncoder::new(config.clone())
                .expect(&format!("Failed to create encoder {}", i));
            
            let result = encoder.encode_frame(&test_frame, 1280, 720);
            assert!(result.is_ok(), "Encoding should succeed in iteration {}", i);
            
            // Encoder should be properly dropped here
        }
        
        // Test that encoder can handle many frames without memory leaks
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        
        for i in 0..100 {
            let result = encoder.encode_frame(&test_frame, 1280, 720);
            assert!(result.is_ok(), "Encoding should succeed in frame {}", i);
            
            // Each frame should be independent - no memory accumulation
        }
        
        // Test cleanup method if available
        encoder.cleanup();
    }

    /// Test thread safety for concurrent encoding
    #[test]
    fn test_thread_safety_concurrent_encoding() {
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 4,
        };
        
        let encoder = Arc::new(parking_lot::Mutex::new(
            FrameEncoder::new(config).expect("Failed to create encoder")
        ));
        
        let test_frame = create_test_bgra_frame(1280, 720);
        let frame_arc = Arc::new(test_frame);
        
        let mut handles = vec![];
        
        // Spawn multiple threads to encode concurrently
        for i in 0..4 {
            let encoder_clone = Arc::clone(&encoder);
            let frame_clone = Arc::clone(&frame_arc);
            
            let handle = thread::spawn(move || {
                for j in 0..10 {
                    let mut encoder_guard = encoder_clone.lock();
                    let result = encoder_guard.encode_frame(&frame_clone, 1280, 720);
                    assert!(
                        result.is_ok(),
                        "Concurrent encoding should succeed in thread {} iteration {}",
                        i, j
                    );
                }
            });
            
            handles.push(handle);
        }
        
        // Wait for all threads to complete
        for handle in handles {
            handle.join().expect("Thread should complete successfully");
        }
    }

    /// Test encoder configuration updates
    #[test]
    fn test_encoder_configuration_updates() {
        let initial_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 50,
            max_threads: 2,
        };
        
        let mut encoder = FrameEncoder::new(initial_config)
            .expect("Failed to create encoder");
        
        let test_frame = create_test_bgra_frame(1280, 720);
        
        // Encode with initial config
        let initial_result = encoder.encode_frame(&test_frame, 1280, 720);
        assert!(initial_result.is_ok(), "Initial encoding should succeed");
        
        // Update configuration
        let updated_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Lz4,
            quality: 90,
            max_threads: 4,
        };
        
        encoder.update_config(updated_config);
        
        // Encode with updated config
        let updated_result = encoder.encode_frame(&test_frame, 1280, 720);
        assert!(updated_result.is_ok(), "Updated encoding should succeed");
        
        let updated_encoded = updated_result.unwrap();
        assert_eq!(updated_encoded.metadata.format, CompressionFormat::Lz4);
    }

    /// Test batch encoding of multiple frames
    #[test]
    fn test_batch_frame_encoding() {
        let mut fixture = FrameEncodingTestFixture::new();
        
        let frames = vec![
            (&fixture.test_frame_1920x1080, 1920u32, 1080u32),
            (&fixture.test_frame_1280x720, 1280u32, 720u32),
            (&fixture.test_frame_640x480, 640u32, 480u32),
        ];
        
        let results = fixture.encoder.encode_batch(&frames);
        assert!(results.is_ok(), "Batch encoding should succeed");
        
        let encoded_frames = results.unwrap();
        assert_eq!(encoded_frames.len(), 3, "Should return 3 encoded frames");
        
        // Verify each frame was encoded correctly
        for (i, encoded_frame) in encoded_frames.iter().enumerate() {
            assert!(!encoded_frame.data.is_empty(), "Frame {} should contain data", i);
            assert!(encoded_frame.metadata.compressed_size > 0, "Frame {} should have compressed size", i);
        }
    }
}

#[cfg(test)]
mod benchmark_tests {
    use super::*;
    use criterion::{criterion_group, criterion_main, Criterion, BenchmarkId};

    /// Benchmark different resolutions
    fn benchmark_encoding_resolutions(c: &mut Criterion) {
        let mut group = c.benchmark_group("encoding_resolutions");
        
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 4,
        };
        
        let resolutions = vec![
            (640, 480, "640x480"),
            (1280, 720, "1280x720"),
            (1920, 1080, "1920x1080"),
            (2560, 1440, "2560x1440"),
        ];
        
        for (width, height, name) in resolutions {
            let test_frame = create_test_bgra_frame(width, height);
            let mut encoder = FrameEncoder::new(config.clone())
                .expect("Failed to create encoder for benchmark");
            
            group.bench_with_input(
                BenchmarkId::new("encode_frame", name),
                &(test_frame, width, height),
                |b, (frame, w, h)| {
                    b.iter(|| {
                        black_box(encoder.encode_frame(black_box(frame), *w, *h))
                    })
                },
            );
        }
        
        group.finish();
    }

    /// Benchmark different compression formats
    fn benchmark_compression_formats(c: &mut Criterion) {
        let mut group = c.benchmark_group("compression_formats");
        
        let test_frame = create_test_bgra_frame(1920, 1080);
        let formats = vec![
            (CompressionFormat::Zlib, "zlib"),
            (CompressionFormat::Lz4, "lz4"),
            (CompressionFormat::Zstd, "zstd"),
        ];
        
        for (format, name) in formats {
            let config = FrameEncodingConfig {
                compression_format: format,
                quality: 80,
                max_threads: 4,
            };
            
            let mut encoder = FrameEncoder::new(config)
                .expect("Failed to create encoder for benchmark");
            
            group.bench_with_input(
                BenchmarkId::new("encode_1920x1080", name),
                &test_frame,
                |b, frame| {
                    b.iter(|| {
                        black_box(encoder.encode_frame(black_box(frame), 1920, 1080))
                    })
                },
            );
        }
        
        group.finish();
    }

    /// Benchmark different quality settings
    fn benchmark_quality_settings(c: &mut Criterion) {
        let mut group = c.benchmark_group("quality_settings");
        
        let test_frame = create_test_bgra_frame(1920, 1080);
        let qualities = vec![20, 40, 60, 80, 95];
        
        for quality in qualities {
            let config = FrameEncodingConfig {
                compression_format: CompressionFormat::Zlib,
                quality,
                max_threads: 4,
            };
            
            let mut encoder = FrameEncoder::new(config)
                .expect("Failed to create encoder for benchmark");
            
            group.bench_with_input(
                BenchmarkId::new("encode_1920x1080", format!("quality_{}", quality)),
                &test_frame,
                |b, frame| {
                    b.iter(|| {
                        black_box(encoder.encode_frame(black_box(frame), 1920, 1080))
                    })
                },
            );
        }
        
        group.finish();
    }

    criterion_group!(
        benches,
        benchmark_encoding_resolutions,
        benchmark_compression_formats,
        benchmark_quality_settings
    );
    criterion_main!(benches);
}

/// Integration test module for frame encoding
#[cfg(test)]
mod integration_tests {
    use super::*;
    use std::fs;
    use std::path::Path;

    /// Test saving and loading encoded frames
    #[test]
    fn test_save_and_load_encoded_frames() {
        let mut fixture = FrameEncodingTestFixture::new();
        
        let result = fixture.encoder.encode_frame(&fixture.test_frame_1280x720, 1280, 720);
        assert!(result.is_ok(), "Encoding should succeed");
        
        let encoded_frame = result.unwrap();
        
        // Save to file
        let temp_path = "/tmp/test_encoded_frame.bin";
        encoded_frame.save_to_file(temp_path)
            .expect("Should be able to save encoded frame");
        
        // Load from file
        let loaded_frame = EncodedFrame::load_from_file(temp_path)
            .expect("Should be able to load encoded frame");
        
        assert_eq!(encoded_frame.data, loaded_frame.data, "Data should match");
        assert_eq!(encoded_frame.metadata.width, loaded_frame.metadata.width, "Width should match");
        assert_eq!(encoded_frame.metadata.height, loaded_frame.metadata.height, "Height should match");
        
        // Cleanup
        let _ = fs::remove_file(temp_path);
    }

    /// Test decoding encoded frames back to raw data
    #[test]
    fn test_encode_decode_roundtrip() {
        let mut fixture = FrameEncodingTestFixture::new();
        let original_frame = &fixture.test_frame_1280x720;
        
        // Encode
        let encode_result = fixture.encoder.encode_frame(original_frame, 1280, 720);
        assert!(encode_result.is_ok(), "Encoding should succeed");
        
        let encoded_frame = encode_result.unwrap();
        
        // Decode
        let decode_result = encoded_frame.decode();
        assert!(decode_result.is_ok(), "Decoding should succeed");
        
        let decoded_frame = decode_result.unwrap();
        assert_eq!(decoded_frame.len(), original_frame.len(), "Decoded frame size should match original");
        
        // For lossless compression, data should match exactly
        if encoded_frame.metadata.format == CompressionFormat::Lz4 {
            assert_eq!(&decoded_frame, original_frame, "Lossless compression should preserve data exactly");
        }
    }
}