//! Comprehensive test suite for frame decoding
//! 
//! This test suite follows Test-Driven Development (TDD) methodology.
//! These tests define the expected behavior BEFORE implementation.

#[cfg(test)]
mod frame_decoding_tests {
    use crate::encoding::{
        FrameEncoder, FrameEncodingConfig, CompressionFormat, EncodedFrame,
        FrameDecoder, DecodedFrame, DecodingError, DecoderConfig
    };
    use std::time::Instant;

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

    /// Test basic frame decoding functionality
    #[test]
    fn test_basic_frame_decoding() {
        // First encode a frame
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frame = create_test_bgra_frame(640, 480);
        let encoded = encoder.encode_frame(&test_frame, 640, 480)
            .expect("Failed to encode frame");
        
        // Now decode it
        let decoder = FrameDecoder::new();
        let result = decoder.decode_frame(&encoded);
        
        assert!(result.is_ok(), "Basic frame decoding should succeed");
        
        let decoded = result.unwrap();
        assert_eq!(decoded.width, 640, "Width should be preserved");
        assert_eq!(decoded.height, 480, "Height should be preserved");
        assert_eq!(decoded.data.len(), test_frame.len(), "Decoded data should match original size");
        assert_eq!(decoded.format, CompressionFormat::Zlib, "Format should be preserved");
        
        // Verify the decoded data matches the original
        assert_eq!(decoded.data, test_frame, "Decoded data should match original");
    }

    /// Test decoding with different compression formats
    #[test]
    fn test_decode_different_formats() {
        let test_frame = create_test_bgra_frame(320, 240);
        let decoder = FrameDecoder::new();
        
        // Test each compression format
        for format in [CompressionFormat::Zlib, CompressionFormat::Lz4, CompressionFormat::Zstd] {
            let config = FrameEncodingConfig {
                compression_format: format,
                quality: 80,
                max_threads: 1,
            };
            
            let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
            let encoded = encoder.encode_frame(&test_frame, 320, 240)
                .expect("Failed to encode frame");
            
            let decoded = decoder.decode_frame(&encoded)
                .expect(&format!("Failed to decode {:?} format", format));
            
            assert_eq!(decoded.data, test_frame, 
                "Decoded {:?} data should match original", format);
        }
    }

    /// Test decoding performance
    #[test]
    fn test_decoding_performance() {
        // Encode a 1920x1080 frame first
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frame = create_test_bgra_frame(1920, 1080);
        let encoded = encoder.encode_frame(&test_frame, 1920, 1080)
            .expect("Failed to encode frame");
        
        // Now test decoding performance
        let decoder = FrameDecoder::new();
        let start = Instant::now();
        let result = decoder.decode_frame(&encoded);
        let duration = start.elapsed();
        
        assert!(result.is_ok(), "1920x1080 decoding should succeed");
        
        let decoded = result.unwrap();
        let duration_ms = duration.as_millis();
        
        println!("1920x1080 decoding took {} ms", duration_ms);
        println!("Decoding duration from metadata: {} μs", decoded.decoding_duration_us);
        
        // Decoding should be faster than encoding (< 20ms)
        assert!(duration_ms < 20, 
            "Decoding should complete in under 20ms, took {}ms", duration_ms);
    }

    /// Test error handling for corrupted data
    #[test]
    fn test_decode_corrupted_data() {
        let decoder = FrameDecoder::new();
        
        // Create a valid encoded frame first
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frame = create_test_bgra_frame(100, 100);
        let mut encoded = encoder.encode_frame(&test_frame, 100, 100)
            .expect("Failed to encode frame");
        
        // Corrupt the data
        if encoded.data.len() > 10 {
            // Corrupt some bytes in the middle
            for i in 5..10 {
                encoded.data[i] = 0xFF;
            }
        }
        
        let result = decoder.decode_frame(&encoded);
        assert!(result.is_err(), "Decoding corrupted data should fail");
        
        // Test with completely random data
        encoded.data = vec![0xFF; 100];
        let result2 = decoder.decode_frame(&encoded);
        assert!(result2.is_err(), "Decoding random data should fail");
    }

    /// Test decoding with size validation
    #[test]
    fn test_decode_size_validation() {
        let decoder = FrameDecoder::new();
        
        // Create an encoded frame with mismatched size metadata
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frame = create_test_bgra_frame(100, 100);
        let mut encoded = encoder.encode_frame(&test_frame, 100, 100)
            .expect("Failed to encode frame");
        
        // Modify metadata to have wrong dimensions
        encoded.metadata.width = 200;
        encoded.metadata.height = 200;
        encoded.metadata.original_size = 200 * 200 * 4;
        
        let result = decoder.decode_frame(&encoded);
        // This might succeed but produce wrong-sized output
        if let Ok(decoded) = result {
            assert_ne!(decoded.data.len(), 200 * 200 * 4,
                "Decoded size should not match corrupted metadata");
        }
    }

    /// Test thread safety of decoder
    #[test]
    fn test_decoder_thread_safety() {
        use std::sync::Arc;
        use std::thread;
        
        // Create some encoded frames
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frames: Vec<_> = (0..4)
            .map(|i| {
                let size = 100 + i * 50;
                let frame = create_test_bgra_frame(size, size);
                encoder.encode_frame(&frame, size, size).unwrap()
            })
            .collect();
        
        let decoder = Arc::new(FrameDecoder::new());
        let handles: Vec<_> = test_frames.into_iter()
            .map(|encoded| {
                let decoder_clone = Arc::clone(&decoder);
                thread::spawn(move || {
                    decoder_clone.decode_frame(&encoded)
                })
            })
            .collect();
        
        // All threads should complete successfully
        for handle in handles {
            let result = handle.join().unwrap();
            assert!(result.is_ok(), "Concurrent decoding should succeed");
        }
    }

    /// Test decoder configuration and options
    #[test]
    fn test_decoder_configuration() {
        // Test with custom configuration
        let config = DecoderConfig {
            max_frame_size: 4096 * 2160 * 4, // 4K limit
            enable_validation: true,
            num_threads: 2,
        };
        
        let decoder = FrameDecoder::with_config(config);
        
        // Test decoding within limits
        let encoder_config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(encoder_config).expect("Failed to create encoder");
        let test_frame = create_test_bgra_frame(1920, 1080);
        let encoded = encoder.encode_frame(&test_frame, 1920, 1080)
            .expect("Failed to encode frame");
        
        let result = decoder.decode_frame(&encoded);
        assert!(result.is_ok(), "Decoding within size limits should succeed");
        
        // Test exceeding size limits
        let large_frame = create_test_bgra_frame(5000, 3000);
        let large_encoded = encoder.encode_frame(&large_frame, 5000, 3000)
            .expect("Failed to encode large frame");
        
        let result = decoder.decode_frame(&large_encoded);
        assert!(result.is_err(), "Decoding exceeding size limits should fail");
    }

    /// Test batch decoding
    #[test]
    fn test_batch_decoding() {
        let decoder = FrameDecoder::new();
        
        // Create multiple encoded frames
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let encoded_frames: Vec<_> = (0..5)
            .map(|i| {
                let size = 200 + i * 100;
                let frame = create_test_bgra_frame(size, size);
                encoder.encode_frame(&frame, size, size).unwrap()
            })
            .collect();
        
        // Decode batch
        let results = decoder.decode_batch(&encoded_frames);
        
        assert_eq!(results.len(), encoded_frames.len(), 
            "Should decode all frames in batch");
        
        for (i, result) in results.iter().enumerate() {
            assert!(result.is_ok(), "Frame {} should decode successfully", i);
            let decoded = result.as_ref().unwrap();
            let expected_size = 200 + i * 100;
            assert_eq!(decoded.width, expected_size as u32);
            assert_eq!(decoded.height, expected_size as u32);
        }
    }

    /// Test round-trip encoding and decoding with data integrity
    #[test]
    fn test_round_trip_integrity() {
        let test_sizes = [(640, 480), (1280, 720), (1920, 1080)];
        let decoder = FrameDecoder::new();
        
        for (width, height) in test_sizes {
            for format in [CompressionFormat::Zlib, CompressionFormat::Lz4, CompressionFormat::Zstd] {
                let config = FrameEncodingConfig {
                    compression_format: format,
                    quality: 90, // High quality for better integrity
                    max_threads: 1,
                };
                
                let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
                let original = create_test_bgra_frame(width, height);
                
                // Encode
                let encoded = encoder.encode_frame(&original, width, height)
                    .expect("Failed to encode");
                
                // Decode
                let decoded = decoder.decode_frame(&encoded)
                    .expect("Failed to decode");
                
                // Verify integrity
                assert_eq!(decoded.width, width);
                assert_eq!(decoded.height, height);
                assert_eq!(decoded.data.len(), original.len());
                assert_eq!(decoded.data, original, 
                    "Round-trip for {}x{} {:?} should preserve data", 
                    width, height, format);
                
                // Check compression was effective
                assert!(encoded.data.len() < original.len(),
                    "Encoded size should be smaller than original");
            }
        }
    }
}