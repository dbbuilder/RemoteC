//! H.265/HEVC video encoder implementation

use super::{VideoEncoder, VideoDecoder, EncoderConfig, EncodedFrame, EncoderStats, DecoderStats, VideoCodec};
use crate::{Result, RemoteCError};
use std::sync::Mutex;

/// H.265 video encoder
pub struct H265Encoder {
    config: Option<EncoderConfig>,
    stats: Mutex<EncoderStats>,
    frame_counter: u64,
}

impl H265Encoder {
    pub fn new() -> Result<Self> {
        Ok(Self {
            config: None,
            stats: Mutex::new(EncoderStats::default()),
            frame_counter: 0,
        })
    }
}

impl VideoEncoder for H265Encoder {
    fn configure(&mut self, config: EncoderConfig) -> Result<()> {
        if config.codec != VideoCodec::H265 {
            return Err(RemoteCError::EncodingError(
                "Invalid codec for H265Encoder".to_string()
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
        
        // TODO: Implement actual H.265 encoding
        self.frame_counter += 1;
        let is_keyframe = self.frame_counter % config.keyframe_interval as u64 == 1;
        
        let mut stats = self.stats.lock().unwrap();
        stats.frames_encoded += 1;
        if is_keyframe {
            stats.keyframes_encoded += 1;
        }
        
        // Placeholder: return dummy encoded data (H.265 is more efficient)
        Ok(EncodedFrame {
            data: vec![0; frame.len() / 150], // Better compression than H.264
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

/// H.265 video decoder
pub struct H265Decoder {
    stats: Mutex<DecoderStats>,
}

impl H265Decoder {
    pub fn new() -> Result<Self> {
        Ok(Self {
            stats: Mutex::new(DecoderStats::default()),
        })
    }
}

impl VideoDecoder for H265Decoder {
    fn configure(&mut self, codec: VideoCodec) -> Result<()> {
        if codec != VideoCodec::H265 {
            return Err(RemoteCError::DecodingError(
                "Invalid codec for H265Decoder".to_string()
            ));
        }
        Ok(())
    }
    
    fn decode_frame(&mut self, frame: &EncodedFrame) -> Result<Vec<u8>> {
        // TODO: Implement actual H.265 decoding
        let mut stats = self.stats.lock().unwrap();
        stats.frames_decoded += 1;
        
        Ok(vec![0; frame.data.len() * 150])
    }
    
    fn get_stats(&self) -> DecoderStats {
        self.stats.lock().unwrap().clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        *self.stats.lock().unwrap() = DecoderStats::default();
        Ok(())
    }
}