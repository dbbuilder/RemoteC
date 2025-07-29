use super::*;
use std::sync::{Arc, Mutex};
use std::time::Instant;

/// Mock video encoder for testing
struct MockVideoEncoder {
    config: Option<EncoderConfig>,
    stats: EncoderStats,
    encode_delay_ms: u64,
    should_fail: bool,
    frame_counter: u64,
}

impl MockVideoEncoder {
    fn new() -> Self {
        Self {
            config: None,
            stats: EncoderStats::default(),
            encode_delay_ms: 0,
            should_fail: false,
            frame_counter: 0,
        }
    }
    
    fn with_delay(mut self, delay_ms: u64) -> Self {
        self.encode_delay_ms = delay_ms;
        self
    }
    
    fn with_failure(mut self) -> Self {
        self.should_fail = true;
        self
    }
}

impl VideoEncoder for MockVideoEncoder {
    fn configure(&mut self, config: EncoderConfig) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::EncodingError("Mock configuration failure".to_string()));
        }
        self.config = Some(config);
        Ok(())
    }
    
    fn encode_frame(&mut self, frame: &[u8], timestamp: u64) -> Result<EncodedFrame> {
        if self.should_fail {
            return Err(RemoteCError::EncodingError("Mock encoding failure".to_string()));
        }
        
        let config = self.config.as_ref()
            .ok_or_else(|| RemoteCError::EncodingError("Encoder not configured".to_string()))?;
        
        // Validate frame size
        let expected_size = (config.width * config.height * 4) as usize; // RGBA
        if frame.len() != expected_size {
            return Err(RemoteCError::EncodingError(
                format!("Invalid frame size: expected {}, got {}", expected_size, frame.len())
            ));
        }
        
        // Simulate encoding delay
        if self.encode_delay_ms > 0 {
            std::thread::sleep(std::time::Duration::from_millis(self.encode_delay_ms));
        }
        
        let start = Instant::now();
        
        // Simulate encoded data (much smaller than raw)
        let encoded_size = frame.len() / 50; // ~2% of original size
        let mut encoded_data = vec![0u8; encoded_size];
        
        // Put some pattern in the data
        for (i, byte) in encoded_data.iter_mut().enumerate() {
            *byte = (i % 256) as u8;
        }
        
        self.frame_counter += 1;
        let is_keyframe = self.frame_counter % config.keyframe_interval as u64 == 1;
        
        self.stats.frames_encoded += 1;
        if is_keyframe {
            self.stats.keyframes_encoded += 1;
        }
        
        let encode_time = start.elapsed().as_micros() as f64;
        self.stats.avg_encode_time = 
            (self.stats.avg_encode_time * (self.stats.frames_encoded - 1) as f64 + encode_time) 
            / self.stats.frames_encoded as f64;
        
        Ok(EncodedFrame {
            data: encoded_data,
            timestamp,
            is_keyframe,
            sequence: self.frame_counter,
        })
    }
    
    fn flush(&mut self) -> Result<Vec<EncodedFrame>> {
        if self.should_fail {
            return Err(RemoteCError::EncodingError("Mock flush failure".to_string()));
        }
        Ok(Vec::new()) // No buffered frames in mock
    }
    
    fn get_stats(&self) -> EncoderStats {
        self.stats.clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::EncodingError("Mock reset failure".to_string()));
        }
        self.stats = EncoderStats::default();
        self.frame_counter = 0;
        Ok(())
    }
}

/// Mock video decoder for testing
struct MockVideoDecoder {
    codec: Option<VideoCodec>,
    stats: DecoderStats,
    should_fail: bool,
}

impl MockVideoDecoder {
    fn new() -> Self {
        Self {
            codec: None,
            stats: DecoderStats::default(),
            should_fail: false,
        }
    }
    
    fn with_failure(mut self) -> Self {
        self.should_fail = true;
        self
    }
}

impl VideoDecoder for MockVideoDecoder {
    fn configure(&mut self, codec: VideoCodec) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::DecodingError("Mock configuration failure".to_string()));
        }
        self.codec = Some(codec);
        Ok(())
    }
    
    fn decode_frame(&mut self, frame: &EncodedFrame) -> Result<Vec<u8>> {
        if self.should_fail {
            self.stats.frames_errors += 1;
            return Err(RemoteCError::DecodingError("Mock decoding failure".to_string()));
        }
        
        let start = Instant::now();
        
        // Simulate decoded frame (expand back to RGBA)
        let decoded_size = frame.data.len() * 50; // Reverse of encoding compression
        let mut decoded_data = vec![0u8; decoded_size];
        
        // Put some pattern based on the frame
        for (i, byte) in decoded_data.iter_mut().enumerate() {
            *byte = ((i + frame.sequence as usize) % 256) as u8;
        }
        
        self.stats.frames_decoded += 1;
        
        let decode_time = start.elapsed().as_micros() as f64;
        self.stats.avg_decode_time = 
            (self.stats.avg_decode_time * (self.stats.frames_decoded - 1) as f64 + decode_time) 
            / self.stats.frames_decoded as f64;
        
        Ok(decoded_data)
    }
    
    fn get_stats(&self) -> DecoderStats {
        self.stats.clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        if self.should_fail {
            return Err(RemoteCError::DecodingError("Mock reset failure".to_string()));
        }
        self.stats = DecoderStats::default();
        Ok(())
    }
}

#[test]
fn test_encoder_config_default() {
    let config = EncoderConfig::default();
    assert_eq!(config.codec, VideoCodec::H264);
    assert_eq!(config.bitrate, 5_000_000);
    assert_eq!(config.framerate, 30);
    assert_eq!(config.width, 1920);
    assert_eq!(config.height, 1080);
    assert_eq!(config.keyframe_interval, 60);
    assert_eq!(config.quality, 75);
    assert!(config.hardware_acceleration);
}

#[test]
fn test_encoder_configuration() {
    let mut encoder = MockVideoEncoder::new();
    
    let config = EncoderConfig {
        codec: VideoCodec::H265,
        bitrate: 10_000_000,
        framerate: 60,
        width: 3840,
        height: 2160,
        keyframe_interval: 120,
        quality: 90,
        hardware_acceleration: false,
    };
    
    assert!(encoder.configure(config.clone()).is_ok());
    assert_eq!(encoder.config.as_ref().unwrap().width, 3840);
    assert_eq!(encoder.config.as_ref().unwrap().height, 2160);
}

#[test]
fn test_encoder_configuration_failure() {
    let mut encoder = MockVideoEncoder::new().with_failure();
    let config = EncoderConfig::default();
    
    assert!(encoder.configure(config).is_err());
}

#[test]
fn test_encode_frame() {
    let mut encoder = MockVideoEncoder::new();
    let config = EncoderConfig {
        width: 640,
        height: 480,
        ..Default::default()
    };
    
    encoder.configure(config).unwrap();
    
    // Create a test frame
    let frame_size = 640 * 480 * 4; // RGBA
    let frame_data = vec![128u8; frame_size];
    
    let result = encoder.encode_frame(&frame_data, 1000);
    assert!(result.is_ok());
    
    let encoded = result.unwrap();
    assert!(encoded.data.len() < frame_data.len()); // Should be compressed
    assert_eq!(encoded.timestamp, 1000);
    assert!(encoded.is_keyframe); // First frame should be keyframe
    assert_eq!(encoded.sequence, 1);
}

#[test]
fn test_encode_frame_invalid_size() {
    let mut encoder = MockVideoEncoder::new();
    let config = EncoderConfig {
        width: 640,
        height: 480,
        ..Default::default()
    };
    
    encoder.configure(config).unwrap();
    
    // Wrong frame size
    let frame_data = vec![0u8; 100];
    
    let result = encoder.encode_frame(&frame_data, 1000);
    assert!(result.is_err());
}

#[test]
fn test_encode_frame_not_configured() {
    let mut encoder = MockVideoEncoder::new();
    let frame_data = vec![0u8; 640 * 480 * 4];
    
    let result = encoder.encode_frame(&frame_data, 1000);
    assert!(result.is_err());
}

#[test]
fn test_keyframe_interval() {
    let mut encoder = MockVideoEncoder::new();
    let config = EncoderConfig {
        width: 320,
        height: 240,
        keyframe_interval: 10,
        ..Default::default()
    };
    
    encoder.configure(config).unwrap();
    
    let frame_size = 320 * 240 * 4;
    let frame_data = vec![0u8; frame_size];
    
    // Encode multiple frames
    let mut keyframe_count = 0;
    for i in 0..30 {
        let encoded = encoder.encode_frame(&frame_data, i * 1000).unwrap();
        if encoded.is_keyframe {
            keyframe_count += 1;
        }
    }
    
    // Should have 3 keyframes (at frames 1, 11, 21)
    assert_eq!(keyframe_count, 3);
}

#[test]
fn test_encoder_statistics() {
    let mut encoder = MockVideoEncoder::new();
    let config = EncoderConfig {
        width: 320,
        height: 240,
        ..Default::default()
    };
    
    encoder.configure(config).unwrap();
    
    let frame_size = 320 * 240 * 4;
    let frame_data = vec![0u8; frame_size];
    
    // Encode several frames
    for i in 0..5 {
        encoder.encode_frame(&frame_data, i * 1000).unwrap();
    }
    
    let stats = encoder.get_stats();
    assert_eq!(stats.frames_encoded, 5);
    assert_eq!(stats.keyframes_encoded, 1); // Only first frame
    assert!(stats.avg_encode_time > 0.0);
}

#[test]
fn test_encoder_reset() {
    let mut encoder = MockVideoEncoder::new();
    let config = EncoderConfig::default();
    encoder.configure(config).unwrap();
    
    let frame_data = vec![0u8; 1920 * 1080 * 4];
    encoder.encode_frame(&frame_data, 1000).unwrap();
    
    assert_eq!(encoder.get_stats().frames_encoded, 1);
    
    encoder.reset().unwrap();
    assert_eq!(encoder.get_stats().frames_encoded, 0);
}

#[test]
fn test_decoder_configuration() {
    let mut decoder = MockVideoDecoder::new();
    assert!(decoder.configure(VideoCodec::H264).is_ok());
}

#[test]
fn test_decode_frame() {
    let mut decoder = MockVideoDecoder::new();
    decoder.configure(VideoCodec::H264).unwrap();
    
    let encoded_frame = EncodedFrame {
        data: vec![0u8; 1000],
        timestamp: 5000,
        is_keyframe: true,
        sequence: 1,
    };
    
    let result = decoder.decode_frame(&encoded_frame);
    assert!(result.is_ok());
    
    let decoded = result.unwrap();
    assert_eq!(decoded.len(), 50000); // 1000 * 50
}

#[test]
fn test_decoder_statistics() {
    let mut decoder = MockVideoDecoder::new();
    decoder.configure(VideoCodec::H264).unwrap();
    
    for i in 0..3 {
        let frame = EncodedFrame {
            data: vec![0u8; 1000],
            timestamp: i * 1000,
            is_keyframe: i == 0,
            sequence: i + 1,
        };
        decoder.decode_frame(&frame).unwrap();
    }
    
    let stats = decoder.get_stats();
    assert_eq!(stats.frames_decoded, 3);
    assert_eq!(stats.frames_errors, 0);
    assert!(stats.avg_decode_time > 0.0);
}

#[test]
fn test_decoder_error_handling() {
    let mut decoder = MockVideoDecoder::new().with_failure();
    decoder.configure(VideoCodec::H264).unwrap(); // Config works
    
    let frame = EncodedFrame {
        data: vec![0u8; 1000],
        timestamp: 0,
        is_keyframe: true,
        sequence: 1,
    };
    
    assert!(decoder.decode_frame(&frame).is_err());
    assert_eq!(decoder.get_stats().frames_errors, 1);
}

#[test]
fn test_all_codecs() {
    let codecs = vec![
        VideoCodec::H264,
        VideoCodec::H265,
        VideoCodec::VP8,
        VideoCodec::VP9,
    ];
    
    for codec in codecs {
        let mut encoder = MockVideoEncoder::new();
        let mut decoder = MockVideoDecoder::new();
        
        let config = EncoderConfig {
            codec,
            width: 320,
            height: 240,
            ..Default::default()
        };
        
        assert!(encoder.configure(config).is_ok());
        assert!(decoder.configure(codec).is_ok());
    }
}

#[test]
fn test_concurrent_encoding() {
    use std::thread;
    
    let encoder = Arc::new(Mutex::new(MockVideoEncoder::new()));
    let config = EncoderConfig {
        width: 320,
        height: 240,
        ..Default::default()
    };
    
    encoder.lock().unwrap().configure(config).unwrap();
    
    let handles: Vec<_> = (0..4).map(|thread_id| {
        let encoder = Arc::clone(&encoder);
        thread::spawn(move || {
            let frame_data = vec![0u8; 320 * 240 * 4];
            for i in 0..5 {
                let timestamp = (thread_id * 1000 + i) as u64;
                encoder.lock().unwrap().encode_frame(&frame_data, timestamp).unwrap();
            }
        })
    }).collect();
    
    for handle in handles {
        handle.join().unwrap();
    }
    
    let stats = encoder.lock().unwrap().get_stats();
    assert_eq!(stats.frames_encoded, 20); // 4 threads * 5 frames
}

#[test]
fn test_performance_tracking() {
    let mut encoder = MockVideoEncoder::new().with_delay(10);
    let config = EncoderConfig {
        width: 320,
        height: 240,
        ..Default::default()
    };
    
    encoder.configure(config).unwrap();
    
    let frame_data = vec![0u8; 320 * 240 * 4];
    let start = Instant::now();
    
    encoder.encode_frame(&frame_data, 0).unwrap();
    
    let elapsed = start.elapsed();
    assert!(elapsed.as_millis() >= 10);
}

