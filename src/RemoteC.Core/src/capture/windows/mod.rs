//! Windows-specific screen capture implementation

mod monitor;
pub use monitor::{enumerate_monitors_windows, get_monitor_at_point_windows};

use crate::capture::{
    CaptureConfig, CaptureMode, CaptureRegion, ScreenCapture, ScreenFrame,
};
use crate::{RemoteCError, Result};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use winapi::shared::minwindef::DWORD;
use winapi::shared::windef::{HDC, HBITMAP};
use winapi::um::wingdi::{
    BitBlt, CreateCompatibleBitmap, CreateCompatibleDC, DeleteDC, DeleteObject, GetDIBits,
    SelectObject, BITMAPINFO, BITMAPINFOHEADER, BI_RGB, DIB_RGB_COLORS, SRCCOPY,
};
use winapi::um::winuser::{GetDC, ReleaseDC};

/// Windows screen capture implementation
pub struct WindowsCapture {
    config: CaptureConfig,
    is_active: Arc<AtomicBool>,
    desktop_dc: Option<HDC>,
    memory_dc: Option<HDC>,
    bitmap: Option<HBITMAP>,
    capture_width: u32,
    capture_height: u32,
}

impl WindowsCapture {
    /// Create a new Windows capture instance
    pub fn new(config: CaptureConfig) -> Result<Self> {
        // Get virtual desktop info to determine capture dimensions
        let desktop = super::monitor::get_virtual_desktop()?;
        
        let (capture_width, capture_height) = match &config.mode {
            CaptureMode::SingleMonitor(idx) => {
                let monitor = desktop.get_monitor(*idx)
                    .ok_or_else(|| RemoteCError::CaptureError("Invalid monitor index".to_string()))?;
                (monitor.bounds.width, monitor.bounds.height)
            }
            CaptureMode::PrimaryMonitor => {
                let monitor = desktop.primary_monitor();
                (monitor.bounds.width, monitor.bounds.height)
            }
            CaptureMode::AllMonitors => {
                (desktop.total_bounds.width, desktop.total_bounds.height)
            }
            CaptureMode::SelectedMonitors(indices) => {
                // Calculate combined bounds
                let mut min_x = i32::MAX;
                let mut min_y = i32::MAX;
                let mut max_x = i32::MIN;
                let mut max_y = i32::MIN;
                
                for idx in indices {
                    if let Some(monitor) = desktop.get_monitor(*idx) {
                        min_x = min_x.min(monitor.bounds.x);
                        min_y = min_y.min(monitor.bounds.y);
                        max_x = max_x.max(monitor.bounds.x + monitor.bounds.width as i32);
                        max_y = max_y.max(monitor.bounds.y + monitor.bounds.height as i32);
                    }
                }
                
                ((max_x - min_x) as u32, (max_y - min_y) as u32)
            }
            CaptureMode::WindowMonitor(_) => {
                // Default to primary monitor for now
                let monitor = desktop.primary_monitor();
                (monitor.bounds.width, monitor.bounds.height)
            }
        };
        
        Ok(Self {
            config,
            is_active: Arc::new(AtomicBool::new(false)),
            desktop_dc: None,
            memory_dc: None,
            bitmap: None,
            capture_width,
            capture_height,
        })
    }

    /// Initialize capture resources
    fn init_capture(&mut self) -> Result<()> {
        unsafe {
            // Get desktop DC
            self.desktop_dc = Some(GetDC(std::ptr::null_mut()));
            if self.desktop_dc.is_none() {
                return Err(RemoteCError::CaptureError(
                    "Failed to get desktop DC".to_string(),
                ));
            }

            // Create compatible DC
            self.memory_dc = Some(CreateCompatibleDC(self.desktop_dc.unwrap()));
            if self.memory_dc.is_none() {
                self.cleanup();
                return Err(RemoteCError::CaptureError(
                    "Failed to create compatible DC".to_string(),
                ));
            }

            // Create bitmap
            self.bitmap = Some(CreateCompatibleBitmap(
                self.desktop_dc.unwrap(),
                self.capture_width as i32,
                self.capture_height as i32,
            ));
            if self.bitmap.is_none() {
                self.cleanup();
                return Err(RemoteCError::CaptureError(
                    "Failed to create bitmap".to_string(),
                ));
            }

            // Select bitmap into memory DC
            SelectObject(self.memory_dc.unwrap(), self.bitmap.unwrap() as *mut _);
        }

        Ok(())
    }

    /// Cleanup capture resources
    fn cleanup(&mut self) {
        unsafe {
            if let Some(bitmap) = self.bitmap.take() {
                DeleteObject(bitmap as *mut _);
            }
            if let Some(dc) = self.memory_dc.take() {
                DeleteDC(dc);
            }
            if let Some(dc) = self.desktop_dc.take() {
                ReleaseDC(std::ptr::null_mut(), dc);
            }
        }
    }

    /// Capture a single frame
    fn capture_frame(&self) -> Result<ScreenFrame> {
        if self.desktop_dc.is_none() || self.memory_dc.is_none() || self.bitmap.is_none() {
            return Err(RemoteCError::CaptureError(
                "Capture not initialized".to_string(),
            ));
        }

        // Determine source coordinates based on capture mode
        let desktop = super::monitor::get_virtual_desktop()?;
        let (src_x, src_y) = match &self.config.mode {
            CaptureMode::SingleMonitor(idx) => {
                let monitor = desktop.get_monitor(*idx)
                    .ok_or_else(|| RemoteCError::CaptureError("Invalid monitor index".to_string()))?;
                (monitor.bounds.x, monitor.bounds.y)
            }
            CaptureMode::PrimaryMonitor => {
                let monitor = desktop.primary_monitor();
                (monitor.bounds.x, monitor.bounds.y)
            }
            CaptureMode::AllMonitors => (desktop.total_bounds.x, desktop.total_bounds.y),
            CaptureMode::SelectedMonitors(_) => {
                // TODO: Handle multiple monitors
                (0, 0)
            }
            CaptureMode::WindowMonitor(_) => (0, 0),
        };

        unsafe {
            // Copy screen to memory DC
            let result = BitBlt(
                self.memory_dc.unwrap(),
                0,
                0,
                self.capture_width as i32,
                self.capture_height as i32,
                self.desktop_dc.unwrap(),
                src_x,
                src_y,
                SRCCOPY,
            );

            if result == 0 {
                return Err(RemoteCError::CaptureError(
                    "BitBlt failed".to_string(),
                ));
            }

            // Prepare bitmap info
            let mut bitmap_info = BITMAPINFO {
                bmiHeader: BITMAPINFOHEADER {
                    biSize: std::mem::size_of::<BITMAPINFOHEADER>() as DWORD,
                    biWidth: self.capture_width as i32,
                    biHeight: -(self.capture_height as i32), // Negative for top-down
                    biPlanes: 1,
                    biBitCount: 32,
                    biCompression: BI_RGB,
                    biSizeImage: 0,
                    biXPelsPerMeter: 0,
                    biYPelsPerMeter: 0,
                    biClrUsed: 0,
                    biClrImportant: 0,
                },
                bmiColors: [std::mem::zeroed()],
            };

            // Calculate buffer size
            let buffer_size = (self.capture_width * self.capture_height * 4) as usize;
            let mut buffer = vec![0u8; buffer_size];

            // Get bitmap bits
            let scan_lines = GetDIBits(
                self.memory_dc.unwrap(),
                self.bitmap.unwrap(),
                0,
                self.capture_height,
                buffer.as_mut_ptr() as *mut _,
                &mut bitmap_info,
                DIB_RGB_COLORS,
            );

            if scan_lines == 0 {
                return Err(RemoteCError::CaptureError(
                    "GetDIBits failed".to_string(),
                ));
            }

            Ok(ScreenFrame {
                width: self.capture_width,
                height: self.capture_height,
                data: buffer,
                timestamp: std::time::Instant::now(),
            })
        }
    }
}

impl ScreenCapture for WindowsCapture {
    fn start(&mut self) -> Result<()> {
        if self.is_active.load(Ordering::Relaxed) {
            return Ok(());
        }

        self.init_capture()?;
        self.is_active.store(true, Ordering::Relaxed);
        Ok(())
    }

    fn stop(&mut self) -> Result<()> {
        self.is_active.store(false, Ordering::Relaxed);
        self.cleanup();
        Ok(())
    }

    fn get_frame(&mut self) -> Result<Option<ScreenFrame>> {
        if !self.is_active.load(Ordering::Relaxed) {
            return Ok(None);
        }

        Ok(Some(self.capture_frame()?))
    }

    fn is_active(&self) -> bool {
        self.is_active.load(Ordering::Relaxed)
    }

    fn config(&self) -> &CaptureConfig {
        &self.config
    }
}

impl Drop for WindowsCapture {
    fn drop(&mut self) {
        self.cleanup();
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_windows_capture_creation() {
        let config = CaptureConfig::default();
        let capture = WindowsCapture::new(config);
        assert!(capture.is_ok());
    }
}