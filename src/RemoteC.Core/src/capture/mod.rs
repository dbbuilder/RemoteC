//! Screen capture module for RemoteC Core
//! 
//! Provides cross-platform screen capture functionality with high performance.

use crate::{Result, RemoteCError};
use std::sync::Arc;

pub mod monitor;
pub use monitor::{Monitor, MonitorBounds, MonitorOrientation, VirtualDesktop};

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
    /// Capture mode
    pub mode: CaptureMode,
    /// Target frames per second
    pub target_fps: u32,
    /// Whether to capture cursor
    pub capture_cursor: bool,
    /// Capture region (None for full screen)
    pub region: Option<CaptureRegion>,
    /// Quality settings
    pub quality: CaptureQuality,
}

/// Capture mode determines which monitors to capture
#[derive(Debug, Clone)]
pub enum CaptureMode {
    /// Capture a single monitor by index
    SingleMonitor(usize),
    /// Capture the primary monitor
    PrimaryMonitor,
    /// Capture all monitors as one combined image
    AllMonitors,
    /// Capture specific monitors by indices
    SelectedMonitors(Vec<usize>),
    /// Capture the monitor containing a specific window
    WindowMonitor(String), // Window title or ID
}

/// Quality settings for capture
#[derive(Debug, Clone, Copy)]
pub struct CaptureQuality {
    /// Enable hardware acceleration if available
    pub use_hardware_acceleration: bool,
    /// Color depth (16, 24, or 32 bits)
    pub color_depth: u8,
    /// Enable frame differencing for optimization
    pub enable_frame_diff: bool,
    /// JPEG quality for compression (0-100)
    pub jpeg_quality: u8,
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
            mode: CaptureMode::PrimaryMonitor,
            target_fps: 30,
            capture_cursor: true,
            region: None,
            quality: CaptureQuality::default(),
        }
    }
}

impl Default for CaptureQuality {
    fn default() -> Self {
        Self {
            use_hardware_acceleration: true,
            color_depth: 32,
            enable_frame_diff: true,
            jpeg_quality: 85,
        }
    }
}

impl CaptureMode {
    /// Get the monitor indices to capture based on the mode
    pub fn get_monitor_indices(&self, desktop: &VirtualDesktop) -> Vec<usize> {
        match self {
            CaptureMode::SingleMonitor(idx) => vec![*idx],
            CaptureMode::PrimaryMonitor => vec![desktop.primary_index],
            CaptureMode::AllMonitors => (0..desktop.monitors.len()).collect(),
            CaptureMode::SelectedMonitors(indices) => indices.clone(),
            CaptureMode::WindowMonitor(_) => {
                // This requires window manager integration
                vec![desktop.primary_index]
            }
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