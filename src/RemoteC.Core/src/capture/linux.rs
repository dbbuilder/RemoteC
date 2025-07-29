//! Linux-specific screen capture implementation

use super::{CaptureConfig, ScreenCapture, ScreenFrame};
use crate::{Result, RemoteCError};

/// Linux screen capture implementation
pub struct LinuxCapture {
    config: CaptureConfig,
    active: bool,
}

impl LinuxCapture {
    /// Create a new Linux screen capture instance
    pub fn new(config: CaptureConfig) -> Result<Self> {
        Ok(Self {
            config,
            active: false,
        })
    }
}

impl ScreenCapture for LinuxCapture {
    fn start(&mut self) -> Result<()> {
        if self.active {
            return Err(RemoteCError::CaptureError("Already capturing".to_string()));
        }
        
        // TODO: Implement X11/Wayland capture
        self.active = true;
        Ok(())
    }
    
    fn stop(&mut self) -> Result<()> {
        if !self.active {
            return Err(RemoteCError::CaptureError("Not capturing".to_string()));
        }
        
        self.active = false;
        Ok(())
    }
    
    fn get_frame(&mut self) -> Result<Option<ScreenFrame>> {
        if !self.active {
            return Err(RemoteCError::CaptureError("Not capturing".to_string()));
        }
        
        // TODO: Implement frame capture
        Ok(None)
    }
    
    fn is_active(&self) -> bool {
        self.active
    }
    
    fn config(&self) -> &CaptureConfig {
        &self.config
    }
}