//! Screen capture module for RemoteC Core
//! 
//! Provides cross-platform screen capture functionality with high performance.

use crate::{Result, RemoteCError};
use std::sync::Arc;

/// Represents a captured screen frame
#[derive(Debug, Clone)]
pub struct ScreenFrame {
    /// Width of the frame in pixels
    pub width: u32,
    /// Height of the frame in pixels
    pub height: u32,
    /// Raw pixel data in BGRA format
    pub data: Vec<u8>,
    /// Timestamp when the frame was captured
    pub timestamp: std::time::Instant,
}

/// Screen capture configuration
#[derive(Debug, Clone)]
pub struct CaptureConfig {
    /// Target display index (0 for primary)
    pub display_index: usize,
    /// Target frames per second
    pub target_fps: u32,
    /// Whether to capture cursor
    pub capture_cursor: bool,
    /// Capture region (None for full screen)
    pub region: Option<CaptureRegion>,
}

/// Defines a region of the screen to capture
#[derive(Debug, Clone, Copy)]
pub struct CaptureRegion {
    /// X coordinate of the top-left corner
    pub x: i32,
    /// Y coordinate of the top-left corner
    pub y: i32,
    /// Width of the region
    pub width: u32,
    /// Height of the region
    pub height: u32,
}

/// Trait for screen capture implementations
pub trait ScreenCapture: Send + Sync {
    /// Start capturing screens
    fn start(&mut self) -> Result<()>;
    
    /// Stop capturing screens
    fn stop(&mut self) -> Result<()>;
    
    /// Get the next captured frame (non-blocking)
    fn get_frame(&mut self) -> Result<Option<ScreenFrame>>;
    
    /// Check if capture is active
    fn is_active(&self) -> bool;
    
    /// Get current configuration
    fn config(&self) -> &CaptureConfig;
}

impl Default for CaptureConfig {
    fn default() -> Self {
        Self {
            display_index: 0,
            target_fps: 30,
            capture_cursor: true,
            region: None,
        }
    }
}

#[cfg(test)]
mod tests;

// Platform-specific implementations
#[cfg(target_os = "windows")]
pub mod windows;

#[cfg(target_os = "linux")]
pub mod linux;

#[cfg(target_os = "macos")]
pub mod macos;

/// Create a platform-specific screen capture instance
pub fn create_capture(config: CaptureConfig) -> Result<Box<dyn ScreenCapture>> {
    #[cfg(target_os = "windows")]
    {
        Ok(Box::new(windows::WindowsCapture::new(config)?))
    }
    
    #[cfg(target_os = "linux")]
    {
        Ok(Box::new(linux::LinuxCapture::new(config)?))
    }
    
    #[cfg(target_os = "macos")]
    {
        Ok(Box::new(macos::MacOSCapture::new(config)?))
    }
    
    #[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
    {
        Err(RemoteCError::UnsupportedPlatform(
            "Screen capture not supported on this platform".to_string()
        ))
    }
}