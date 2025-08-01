//! Minimal test to verify frame decoder works

#[cfg(test)]
mod decoder_only_tests {
    use remotec_core::encoding::{
        FrameEncoder, FrameEncodingConfig, CompressionFormat, 
        FrameDecoder
    };

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
    fn test_basic_decode() {
        // Create encoder
        let config = FrameEncodingConfig {
            compression_format: CompressionFormat::Zlib,
            quality: 80,
            max_threads: 1,
        };
        
        let mut encoder = FrameEncoder::new(config).expect("Failed to create encoder");
        
        // Encode a frame
        let test_frame = create_test_frame(320, 240);
        let encoded = encoder.encode_frame(&test_frame, 320, 240)
            .expect("Failed to encode frame");
        
        println!("Original size: {} bytes", test_frame.len());
        println!("Encoded size: {} bytes", encoded.data.len());
        println!("Compression ratio: {:.2}", encoded.metadata.compression_ratio);
        
        // Decode the frame
        let decoder = FrameDecoder::new();
        let decoded = decoder.decode_frame(&encoded)
            .expect("Failed to decode frame");
        
        // Verify
        assert_eq!(decoded.width, 320);
        assert_eq!(decoded.height, 240);
        assert_eq!(decoded.data.len(), test_frame.len());
        assert_eq!(decoded.data, test_frame, "Decoded data should match original");
        
        println!("✓ Decoding successful!");
        println!("  Decoding took {} μs", decoded.decoding_duration_us);
    }
}