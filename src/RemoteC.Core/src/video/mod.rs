//! Video encoding and compression module for RemoteC Core
//!
//! Provides high-performance video encoding using H.264/H.265 codecs.

use crate::{Result, RemoteCError};
use std::sync::Arc;

/// Supported video codecs
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum VideoCodec {
    /// H.264/AVC codec - best compatibility
    H264,
    /// H.265/HEVC codec - better compression
    H265,
    /// VP8 codec - WebRTC compatibility
    VP8,
    /// VP9 codec - better quality than VP8
    VP9,
}

/// Video encoder configuration
#[derive(Debug, Clone)]
pub struct EncoderConfig {
    /// Target codec
    pub codec: VideoCodec,
    /// Target bitrate in bits per second
    pub bitrate: u32,
    /// Target frame rate
    pub framerate: u32,
    /// Video width
    pub width: u32,
    /// Video height
    pub height: u32,
    /// Keyframe interval (in frames)
    pub keyframe_interval: u32,
    /// Quality preset (0-100, higher is better quality)
    pub quality: u8,
    /// Enable hardware acceleration if available
    pub hardware_acceleration: bool,
}

impl Default for EncoderConfig {
    fn default() -> Self {
        Self {
            codec: VideoCodec::H264,
            bitrate: 5_000_000, // 5 Mbps
            framerate: 30,
            width: 1920,
            height: 1080,
            keyframe_interval: 60, // 2 seconds at 30fps
            quality: 75,
            hardware_acceleration: true,
        }
    }
}

/// Encoded video frame
#[derive(Debug)]
pub struct EncodedFrame {
    /// Frame data
    pub data: Vec<u8>,
    /// Frame timestamp in microseconds
    pub timestamp: u64,
    /// Is this a keyframe?
    pub is_keyframe: bool,
    /// Frame sequence number
    pub sequence: u64,
}

/// Video encoder trait
pub trait VideoEncoder: Send + Sync {
    /// Configure the encoder
    fn configure(&mut self, config: EncoderConfig) -> Result<()>;
    
    /// Encode a raw frame (RGBA format)
    fn encode_frame(&mut self, frame: &[u8], timestamp: u64) -> Result<EncodedFrame>;
    
    /// Flush any pending frames
    fn flush(&mut self) -> Result<Vec<EncodedFrame>>;
    
    /// Get current encoder statistics
    fn get_stats(&self) -> EncoderStats;
    
    /// Reset the encoder
    fn reset(&mut self) -> Result<()>;
}

/// Encoder statistics
#[derive(Debug, Default, Clone)]
pub struct EncoderStats {
    /// Total frames encoded
    pub frames_encoded: u64,
    /// Total keyframes encoded
    pub keyframes_encoded: u64,
    /// Average encoding time in microseconds
    pub avg_encode_time: f64,
    /// Current bitrate (bits per second)
    pub current_bitrate: u32,
    /// Frames dropped due to performance
    pub frames_dropped: u64,
}

/// Video decoder trait
pub trait VideoDecoder: Send + Sync {
    /// Configure the decoder
    fn configure(&mut self, codec: VideoCodec) -> Result<()>;
    
    /// Decode an encoded frame
    fn decode_frame(&mut self, frame: &EncodedFrame) -> Result<Vec<u8>>;
    
    /// Get decoder statistics
    fn get_stats(&self) -> DecoderStats;
    
    /// Reset the decoder
    fn reset(&mut self) -> Result<()>;
}

/// Decoder statistics
#[derive(Debug, Default, Clone)]
pub struct DecoderStats {
    /// Total frames decoded
    pub frames_decoded: u64,
    /// Frames with errors
    pub frames_errors: u64,
    /// Average decoding time in microseconds
    pub avg_decode_time: f64,
}

#[cfg(test)]
mod tests;

// Platform-specific implementations
mod h264;
mod h265;
mod vp8;
mod vp9;

pub use h264::H264Encoder;
pub use h265::H265Encoder;
pub use vp8::VP8Encoder;
pub use vp9::VP9Encoder;

/// Create a video encoder for the specified codec
pub fn create_encoder(codec: VideoCodec) -> Result<Box<dyn VideoEncoder>> {
    match codec {
        VideoCodec::H264 => Ok(Box::new(H264Encoder::new()?)),
        VideoCodec::H265 => Ok(Box::new(H265Encoder::new()?)),
        VideoCodec::VP8 => Ok(Box::new(VP8Encoder::new()?)),
        VideoCodec::VP9 => Ok(Box::new(VP9Encoder::new()?)),
    }
}

/// Create a video decoder for the specified codec
pub fn create_decoder(codec: VideoCodec) -> Result<Box<dyn VideoDecoder>> {
    match codec {
        VideoCodec::H264 => Ok(Box::new(h264::H264Decoder::new()?)),
        VideoCodec::H265 => Ok(Box::new(h265::H265Decoder::new()?)),
        VideoCodec::VP8 => Ok(Box::new(vp8::VP8Decoder::new()?)),
        VideoCodec::VP9 => Ok(Box::new(vp9::VP9Decoder::new()?)),
    }
}