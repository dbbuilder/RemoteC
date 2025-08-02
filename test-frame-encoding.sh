#!/bin/bash

echo "=== Testing RemoteC Frame Encoding ==="
echo ""

# Create a simple Rust test program
cat > /tmp/test_encoding.rs << 'EOF'
use std::time::Instant;

// FFI declarations for the Rust library
#[repr(C)]
pub struct FrameEncodingConfig {
    compression_format: u32, // 0=None, 1=Zlib, 2=LZ4, 3=Zstd
    quality: u8,
    max_threads: usize,
}

#[repr(C)]
pub struct FrameMetadata {
    width: u32,
    height: u32,
    format: u32,
    original_size: usize,
    compressed_size: usize,
    compression_ratio: f32,
    timestamp: u64,
    encoding_duration_us: u64,
}

#[repr(C)]
pub struct EncodedFrame {
    data: *mut u8,
    data_len: usize,
    metadata: FrameMetadata,
}

#[link(name = "remotec_core")]
extern "C" {
    fn remotec_encoder_create(config: *const FrameEncodingConfig) -> *mut std::ffi::c_void;
    fn remotec_encoder_destroy(encoder: *mut std::ffi::c_void);
    fn remotec_encoder_encode_frame(
        encoder: *mut std::ffi::c_void,
        data: *const u8,
        data_len: usize,
        width: u32,
        height: u32,
        frame: *mut EncodedFrame,
    ) -> i32;
    fn remotec_frame_free(frame: *mut EncodedFrame);
}

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

fn main() {
    unsafe {
        println!("Creating encoder with Zlib compression...");
        let config = FrameEncodingConfig {
            compression_format: 1, // Zlib
            quality: 80,
            max_threads: 1,
        };
        
        let encoder = remotec_encoder_create(&config as *const _);
        if encoder.is_null() {
            eprintln!("Failed to create encoder");
            return;
        }
        
        println!("✓ Encoder created");
        
        // Test 1: Small frame
        println!("\nTest 1: 640x480 frame");
        let test_frame = create_test_frame(640, 480);
        let mut encoded_frame = std::mem::zeroed::<EncodedFrame>();
        
        let start = Instant::now();
        let result = remotec_encoder_encode_frame(
            encoder,
            test_frame.as_ptr(),
            test_frame.len(),
            640,
            480,
            &mut encoded_frame as *mut _,
        );
        let duration = start.elapsed();
        
        if result == 0 {
            println!("✓ Encoding successful");
            println!("  Original size: {} bytes", encoded_frame.metadata.original_size);
            println!("  Compressed size: {} bytes", encoded_frame.metadata.compressed_size);
            println!("  Compression ratio: {:.2}x", encoded_frame.metadata.compression_ratio);
            println!("  Encoding time: {} μs (measured: {} μs)", 
                encoded_frame.metadata.encoding_duration_us,
                duration.as_micros());
            
            remotec_frame_free(&mut encoded_frame as *mut _);
        } else {
            eprintln!("✗ Encoding failed with error: {}", result);
        }
        
        // Test 2: HD frame
        println!("\nTest 2: 1920x1080 frame");
        let test_frame_hd = create_test_frame(1920, 1080);
        let mut encoded_frame_hd = std::mem::zeroed::<EncodedFrame>();
        
        let start_hd = Instant::now();
        let result_hd = remotec_encoder_encode_frame(
            encoder,
            test_frame_hd.as_ptr(),
            test_frame_hd.len(),
            1920,
            1080,
            &mut encoded_frame_hd as *mut _,
        );
        let duration_hd = start_hd.elapsed();
        
        if result_hd == 0 {
            println!("✓ HD encoding successful");
            println!("  Original size: {} bytes", encoded_frame_hd.metadata.original_size);
            println!("  Compressed size: {} bytes", encoded_frame_hd.metadata.compressed_size);
            println!("  Compression ratio: {:.2}x", encoded_frame_hd.metadata.compression_ratio);
            println!("  Encoding time: {} μs (measured: {} ms)", 
                encoded_frame_hd.metadata.encoding_duration_us,
                duration_hd.as_millis());
            
            // Check performance requirement
            if duration_hd.as_millis() < 50 {
                println!("✓ Performance requirement met: < 50ms");
            } else {
                println!("✗ Performance requirement NOT met: {} ms > 50ms", duration_hd.as_millis());
            }
            
            remotec_frame_free(&mut encoded_frame_hd as *mut _);
        } else {
            eprintln!("✗ HD encoding failed with error: {}", result_hd);
        }
        
        // Cleanup
        remotec_encoder_destroy(encoder);
        println!("\n✓ Encoder destroyed");
    }
}
EOF

# Check if we can compile and run it
if command -v rustc &> /dev/null; then
    echo "Compiling test program..."
    if rustc /tmp/test_encoding.rs -o /tmp/test_encoding -L src/RemoteC.Core/target/release -l remotec_core 2>/dev/null; then
        echo "Running test..."
        LD_LIBRARY_PATH=src/RemoteC.Core/target/release /tmp/test_encoding
    else
        echo "Failed to compile test program. This is expected if FFI exports aren't implemented yet."
    fi
else
    echo "Rust compiler not available. Skipping FFI test."
fi

echo ""
echo "Running Rust unit tests for frame encoding..."
cd src/RemoteC.Core && cargo test basic_frame_encoding_test::test_basic_encoding -- --nocapture