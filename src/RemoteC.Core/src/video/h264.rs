//! H.264/AVC video encoder implementation

use super::{VideoEncoder, VideoDecoder, EncoderConfig, EncodedFrame, EncoderStats, DecoderStats, VideoCodec};
use crate::{Result, RemoteCError};
use std::sync::Mutex;
use std::time::Instant;

#[cfg(feature = "openh264")]
use openh264::{encoder::Encoder, formats::YUVSource};
#[cfg(feature = "openh264")]
use openh264::formats::YUVBuffer;

/// H.264 video encoder
pub struct H264Encoder {
    config: Option<EncoderConfig>,
    stats: Mutex<EncoderStats>,
    frame_counter: u64,
    #[cfg(feature = "openh264")]
    encoder: Option<Encoder>,
}

impl H264Encoder {
    pub fn new() -> Result<Self> {
        Ok(Self {
            config: None,
            stats: Mutex::new(EncoderStats::default()),
            frame_counter: 0,
            #[cfg(feature = "openh264")]
            encoder: None,
        })
    }
    
    #[cfg(feature = "openh264")]
    fn create_encoder(config: &EncoderConfig) -> Result<Encoder> {
        use openh264::encoder::EncoderConfig as OpenH264Config;
        
        let mut h264_config = OpenH264Config::new(config.width, config.height);
        h264_config.set_bitrate_bps(config.bitrate);
        // Configure encoder settings
        // Note: openh264 0.4 uses a builder pattern, not setters
        h264_config.enable_skip_frame(true);
        
        // Set quality preset - complexity mode might not be available in this version
        // TODO: Check openh264 API for quality settings
        
        Encoder::with_config(h264_config)
            .map_err(|e| RemoteCError::EncodingError(format!("Failed to create OpenH264 encoder: {:?}", e)))
    }
    
    #[cfg(not(feature = "openh264"))]
    fn encode_frame_internal(&mut self, _frame: &[u8], _timestamp: u64) -> Result<Vec<u8>> {
        // Fallback implementation
        Ok(vec![0; 100])
    }
    
    #[cfg(feature = "openh264")]
    fn encode_frame_internal(&mut self, frame: &[u8], _timestamp: u64) -> Result<Vec<u8>> {
        let config = self.config.as_ref().unwrap();
        let encoder = self.encoder.as_mut().unwrap();
        
        // Convert RGBA to RGB first (YUVBuffer expects RGB)
        let rgb_size = (config.width * config.height * 3) as usize;
        let mut rgb = Vec::with_capacity(rgb_size);
        
        // Strip alpha channel
        for chunk in frame.chunks(4) {
            rgb.push(chunk[0]); // R
            rgb.push(chunk[1]); // G
            rgb.push(chunk[2]); // B
            // Skip chunk[3] (A)
        }
        
        let source = YUVBuffer::with_rgb(config.width as usize, config.height as usize, &rgb);
        
        // Encode frame
        match encoder.encode(&source) {
            Ok(bitstream) => Ok(bitstream.to_vec()),
            Err(e) => Err(RemoteCError::EncodingError(format!("OpenH264 encode failed: {:?}", e))),
        }
    }
}

impl VideoEncoder for H264Encoder {
    fn configure(&mut self, config: EncoderConfig) -> Result<()> {
        if config.codec != VideoCodec::H264 {
            return Err(RemoteCError::EncodingError(
                "Invalid codec for H264Encoder".to_string()
            ));
        }
        
        #[cfg(feature = "openh264")]
        {
            self.encoder = Some(Self::create_encoder(&config)?);
        }
        
        log::info!("H.264 encoder configured: {}x{} @ {} bps", 
                   config.width, config.height, config.bitrate);
        self.config = Some(config);
        Ok(())
    }
    
    fn encode_frame(&mut self, frame: &[u8], timestamp: u64) -> Result<EncodedFrame> {
        let (width, height, keyframe_interval) = {
            let config = self.config.as_ref()
                .ok_or_else(|| RemoteCError::EncodingError("Encoder not configured".to_string()))?;
            
            // Validate frame size
            let expected_size = (config.width * config.height * 4) as usize;
            if frame.len() != expected_size {
                return Err(RemoteCError::EncodingError(
                    format!("Invalid frame size: expected {}, got {}", expected_size, frame.len())
                ));
            }
            
            (config.width, config.height, config.keyframe_interval)
        };
        
        let start = Instant::now();
        
        // Encode frame
        let encoded_data = self.encode_frame_internal(frame, timestamp)?;
        
        self.frame_counter += 1;
        let is_keyframe = self.frame_counter % keyframe_interval as u64 == 1;
        
        // Update statistics
        let encode_time = start.elapsed().as_micros() as f64;
        let mut stats = self.stats.lock().unwrap();
        stats.frames_encoded += 1;
        if is_keyframe {
            stats.keyframes_encoded += 1;
        }
        stats.avg_encode_time = 
            (stats.avg_encode_time * (stats.frames_encoded - 1) as f64 + encode_time) 
            / stats.frames_encoded as f64;
        if let Some(config) = &self.config {
            stats.current_bitrate = config.bitrate;
        }
        
        Ok(EncodedFrame {
            data: encoded_data,
            timestamp,
            is_keyframe,
            sequence: self.frame_counter,
        })
    }
    
    fn flush(&mut self) -> Result<Vec<EncodedFrame>> {
        // TODO: Implement flush
        Ok(Vec::new())
    }
    
    fn get_stats(&self) -> EncoderStats {
        self.stats.lock().unwrap().clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        self.frame_counter = 0;
        *self.stats.lock().unwrap() = EncoderStats::default();
        
        #[cfg(feature = "openh264")]
        if let Some(config) = &self.config {
            self.encoder = Some(Self::create_encoder(config)?);
        }
        
        Ok(())
    }
}

/// Convert RGBA to YUV420 (I420) format
#[cfg(feature = "openh264")]
fn rgba_to_yuv420(rgba: &[u8], width: u32, height: u32) -> Result<Vec<u8>> {
    let width = width as usize;
    let height = height as usize;
    
    // YUV420 size: Y plane (width*height) + U plane (width*height/4) + V plane (width*height/4)
    let yuv_size = width * height + (width * height) / 2;
    let mut yuv = vec![0u8; yuv_size];
    
    let (y_plane, uv_plane) = yuv.split_at_mut(width * height);
    let (u_plane, v_plane) = uv_plane.split_at_mut(width * height / 4);
    
    // Convert RGBA to YUV420
    for y in 0..height {
        for x in 0..width {
            let rgba_idx = (y * width + x) * 4;
            let r = rgba[rgba_idx] as i32;
            let g = rgba[rgba_idx + 1] as i32;
            let b = rgba[rgba_idx + 2] as i32;
            
            // BT.601 conversion
            let y_val = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
            y_plane[y * width + x] = y_val.clamp(0, 255) as u8;
            
            // Subsample U and V (every 2x2 block)
            if x % 2 == 0 && y % 2 == 0 {
                let u_val = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
                let v_val = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;
                
                let uv_idx = (y / 2) * (width / 2) + (x / 2);
                u_plane[uv_idx] = u_val.clamp(0, 255) as u8;
                v_plane[uv_idx] = v_val.clamp(0, 255) as u8;
            }
        }
    }
    
    Ok(yuv)
}

/// H.264 video decoder
pub struct H264Decoder {
    stats: Mutex<DecoderStats>,
}

impl H264Decoder {
    pub fn new() -> Result<Self> {
        Ok(Self {
            stats: Mutex::new(DecoderStats::default()),
        })
    }
}

impl VideoDecoder for H264Decoder {
    fn configure(&mut self, codec: VideoCodec) -> Result<()> {
        if codec != VideoCodec::H264 {
            return Err(RemoteCError::DecodingError(
                "Invalid codec for H264Decoder".to_string()
            ));
        }
        Ok(())
    }
    
    fn decode_frame(&mut self, frame: &EncodedFrame) -> Result<Vec<u8>> {
        // TODO: Implement actual H.264 decoding
        let mut stats = self.stats.lock().unwrap();
        stats.frames_decoded += 1;
        
        // Placeholder: return dummy decoded data
        Ok(vec![0; frame.data.len() * 100]) // Simulate decompression
    }
    
    fn get_stats(&self) -> DecoderStats {
        self.stats.lock().unwrap().clone()
    }
    
    fn reset(&mut self) -> Result<()> {
        *self.stats.lock().unwrap() = DecoderStats::default();
        Ok(())
    }
}