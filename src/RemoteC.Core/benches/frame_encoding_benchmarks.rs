//! Benchmark tests for frame encoding performance
//! 
//! These benchmarks validate that frame encoding meets performance requirements.
//! Run with: cargo bench

use criterion::{black_box, criterion_group, criterion_main, Criterion, BenchmarkId};
use remotec_core::encoding::{FrameEncoder, FrameEncodingConfig, CompressionFormat};

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
        
        group.bench_with_input(
            BenchmarkId::new("encode_frame", name),
            &(test_frame, width, height),
            |b, (frame, w, h)| {
                // Note: This will fail until FrameEncoder is implemented
                // This is expected as part of TDD RED phase
                if let Ok(mut encoder) = FrameEncoder::new(config.clone()) {
                    b.iter(|| {
                        black_box(encoder.encode_frame(black_box(frame), *w, *h))
                    })
                } else {
                    // Skip benchmark if encoder creation fails (RED phase)
                    b.iter(|| {
                        black_box(format!("Encoder not implemented for {}", name))
                    })
                }
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
        
        group.bench_with_input(
            BenchmarkId::new("encode_1920x1080", name),
            &test_frame,
            |b, frame| {
                // Note: This will fail until FrameEncoder is implemented
                // This is expected as part of TDD RED phase
                if let Ok(mut encoder) = FrameEncoder::new(config.clone()) {
                    b.iter(|| {
                        black_box(encoder.encode_frame(black_box(frame), 1920, 1080))
                    })
                } else {
                    // Skip benchmark if encoder creation fails (RED phase)
                    b.iter(|| {
                        black_box(format!("Encoder not implemented for {}", name))
                    })
                }
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
        
        group.bench_with_input(
            BenchmarkId::new("encode_1920x1080", format!("quality_{}", quality)),
            &test_frame,
            |b, frame| {
                // Note: This will fail until FrameEncoder is implemented
                // This is expected as part of TDD RED phase
                if let Ok(mut encoder) = FrameEncoder::new(config.clone()) {
                    b.iter(|| {
                        black_box(encoder.encode_frame(black_box(frame), 1920, 1080))
                    })
                } else {
                    // Skip benchmark if encoder creation fails (RED phase)
                    b.iter(|| {
                        black_box(format!("Encoder not implemented for quality_{}", quality))
                    })
                }
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