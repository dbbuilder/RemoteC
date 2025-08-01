//! Basic frame encoding tests that work with current implementation

#[cfg(test)]
mod basic_frame_encoding_tests {
    use crate::encoding::{FrameEncoder, FrameEncodingConfig, CompressionFormat};
    use std::time::Instant;

    /// Create a test BGRA frame
    fn create_test_frame(width: u32, height: u32) -> Vec<u8> {
        let mut frame = Vec::with_capacity((width * height * 4) as usize);
        
        for y in 0..height {
            for x in 0..width {
                let r = ((x * 255) / width) as u8;
                let g = ((y * 255) / height) as u8;
                let b = ((x + y) % 256) as u8;
                let a = 255u8;
                
                // BGRA format
                frame.push(b);
                frame.push(g);
                frame.push(r);
                frame.push(a);
            }
        }
        
        frame
    }

    #[test]
    fn test_basic_encoding() {
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frame = create_test_frame(640, 480);
        
        let result = encoder.encode_frame(&test_frame, 640, 480);
        assert!(result.is_ok(), "Basic encoding should succeed");
        
        let encoded = result.unwrap();
        assert!(!encoded.data.is_empty(), "Encoded data should not be empty");
        assert_eq!(encoded.metadata.width, 640);
        assert_eq!(encoded.metadata.height, 480);
        assert_eq!(encoded.metadata.format, CompressionFormat::Zlib);
        
        println!("Original size: {} bytes", encoded.metadata.original_size);
        println!("Compressed size: {} bytes", encoded.metadata.compressed_size);
        println!("Compression ratio: {:.2}", encoded.metadata.compression_ratio);
        println!("Encoding time: {} μs", encoded.metadata.encoding_duration_us);
    }

    #[test]
    fn test_different_qualities() {
        let test_frame = create_test_frame(800, 600);
        
        // Test low quality (high compression)
        let mut low_encoder = FrameEncoder::new(FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 20,
            max_threads: 1,
        }).expect("Failed to create low quality encoder");
        
        // Test high quality (low compression)
        let mut high_encoder = FrameEncoder::new(FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 90,
            max_threads: 1,
        }).expect("Failed to create high quality encoder");
        
        let low_result = low_encoder.encode_frame(&test_frame, 800, 600).unwrap();
        let high_result = high_encoder.encode_frame(&test_frame, 800, 600).unwrap();
        
        println!("Low quality size: {} bytes", low_result.data.len());
        println!("High quality size: {} bytes", high_result.data.len());
        
        assert!(low_result.data.len() <= high_result.data.len(), 
            "Low quality should produce smaller or equal size");
    }

    #[test]
    fn test_performance() {
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        let test_frame = create_test_frame(1920, 1080);
        
        let start = Instant::now();
        let result = encoder.encode_frame(&test_frame, 1920, 1080);
        let duration = start.elapsed();
        
        assert!(result.is_ok(), "1920x1080 encoding should succeed");
        
        let encoded = result.unwrap();
        let duration_ms = duration.as_millis();
        
        println!("1920x1080 encoding took {} ms", duration_ms);
        println!("Encoded size: {} bytes", encoded.data.len());
        
        // Check performance requirement
        assert!(duration_ms < 50, "Encoding should complete in under 50ms, took {}ms", duration_ms);
    }

    #[test]
    fn test_invalid_input() {
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        
        // Test with wrong data size
        let small_data = vec![0u8; 100];
        let result = encoder.encode_frame(&small_data, 640, 480);
        assert!(result.is_err(), "Should fail with insufficient data");
        
        // Test with zero dimensions
        let test_frame = create_test_frame(100, 100);
        let zero_width = encoder.encode_frame(&test_frame, 0, 100);
        assert!(zero_width.is_err(), "Should fail with zero width");
        
        let zero_height = encoder.encode_frame(&test_frame, 100, 0);
        assert!(zero_height.is_err(), "Should fail with zero height");
    }
}