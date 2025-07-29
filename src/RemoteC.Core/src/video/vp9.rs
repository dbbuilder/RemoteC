//! VP9 video encoder implementation

use super::{VideoEncoder, VideoDecoder, EncoderConfig, EncodedFrame, EncoderStats, DecoderStats, VideoCodec};
use crate::{Result, RemoteCError};
use std::sync::Mutex;

/// VP9 video encoder
pub struct VP9Encoder {
    config: Option<EncoderConfig>,
    stats: Mutex<EncoderStats>,
    frame_counter: u64,
}

impl VP9Encoder {
    pub fn new() -> Result<Self> {
        Ok(Self {
            config: None,
            stats: Mutex::new(EncoderStats::default()),
            frame_counter: 0,
        })
    }
}

impl VideoEncoder for VP9Encoder {
    fn configure(&mut self, config: EncoderConfig) -> Result<()> {
        if config.codec != VideoCodec::VP9 {
            return Err(RemoteCError::EncodingError(
                "Invalid codec for VP9Encoder".to_string()
            ));
        }
        self.config = Some(config);
        Ok(())
    }
    
    fn encode_frame(&mut self, frame: &[u8], timestamp: u64) -> Result<EncodedFrame> {
        let config = self.config.as_ref()
            .ok_or_else(|| RemoteCError::EncodingError("Encoder not configured".to_string()))?;
        
        // Validate frame size
        let expected_size = (config.width * config.height * 4) as usize;
        if frame.len() != expected_size {
            return Err(RemoteCError::EncodingError(
                format!("Invalid frame size: expected {}, got {}", expected_size, frame.len())
            ));
        }
        
        // TODO: Implement actual VP9 encoding using libvpx
        self.frame_counter += 1;
        let is_keyframe = self.frame_counter % config.keyframe_interval as u64 == 1;
        
        let mut stats = self.stats.lock().unwrap();
        stats.frames_encoded += 1;
        if is_keyframe {
            stats.keyframes_encoded += 1;
        }
        
        Ok(EncodedFrame {
            data: vec![0; frame.len() / 120], // VP9 better compression than VP8
            timestamp,
            is_keyframe,
            sequence: self.frame_counter,
        })
    }
    
    fn flush(&mut self) -> Result<Vec<EncodedFrame>> {
        Ok(Vec::new())
    }
    
    fn get_stats(&self) -> EncoderStats {
        self.stats.lock().unwrap().clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        self.frame_counter = 0;
        *self.stats.lock().unwrap() = EncoderStats::default();
        Ok(())
    }
}

/// VP9 video decoder
pub struct VP9Decoder {
    stats: Mutex<DecoderStats>,
}

impl VP9Decoder {
    pub fn new() -> Result<Self> {
        Ok(Self {
            stats: Mutex::new(DecoderStats::default()),
        })
    }
}

impl VideoDecoder for VP9Decoder {
    fn configure(&mut self, codec: VideoCodec) -> Result<()> {
        if codec != VideoCodec::VP9 {
            return Err(RemoteCError::DecodingError(
                "Invalid codec for VP9Decoder".to_string()
            ));
        }
        Ok(())
    }
    
    fn decode_frame(&mut self, frame: &EncodedFrame) -> Result<Vec<u8>> {
        // TODO: Implement actual VP9 decoding
        let mut stats = self.stats.lock().unwrap();
        stats.frames_decoded += 1;
        
        Ok(vec![0; frame.data.len() * 120])
    }
    
    fn get_stats(&self) -> DecoderStats {
        self.stats.lock().unwrap().clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        *self.stats.lock().unwrap() = DecoderStats::default();
        Ok(())
    }
}