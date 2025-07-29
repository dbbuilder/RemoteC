//! Video encoding module for RemoteC Core

use crate::Result;

pub struct EncodingConfig {
    pub codec: VideoCodec,
    pub bitrate: u32,
    pub framerate: u32,
    pub quality: EncodingQuality,
}

#[derive(Debug, Clone, Copy)]
pub enum VideoCodec {
    H264,
    H265,
    VP8,
    VP9,
}

#[derive(Debug, Clone, Copy)]
pub enum EncodingQuality {
    Low,
    Medium,
    High,
    Lossless,
}

pub trait VideoEncoder: Send + Sync {
    fn encode_frame(&mut self, frame: &[u8], width: u32, height: u32) -> Result<Vec<u8>>;
    fn flush(&mut self) -> Result<Vec<u8>>;
}