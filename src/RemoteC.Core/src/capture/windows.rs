//! Windows-specific screen capture implementation

use super::{CaptureConfig, ScreenCapture, ScreenFrame};
use crate::{Result, RemoteCError};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::{Duration, Instant};

#[cfg(target_os = "windows")]
use winapi::{
    shared::windef::{HDC, HBITMAP},
    um::wingdi::*,
    um::winuser::*,
};

/// Windows screen capture implementation
pub struct WindowsCapture {
    config: CaptureConfig,
    active: Arc<AtomicBool>,
    capture_thread: Option<thread::JoinHandle<()>>,
    frame_buffer: Arc<Mutex<Option<ScreenFrame>>>,
}

impl WindowsCapture {
    /// Create a new Windows screen capture instance
    pub fn new(config: CaptureConfig) -> Result<Self> {
        Ok(Self {
            config,
            active: Arc::new(AtomicBool::new(false)),
            capture_thread: None,
            frame_buffer: Arc::new(Mutex::new(None)),
        })
    }
    
    #[cfg(target_os = "windows")]
    unsafe fn capture_screen(&self) -> Result<ScreenFrame> {
        use std::ptr::null_mut;
        
        // Get the device context of the screen
        let h_screen = GetDC(null_mut());
        if h_screen.is_null() {
            return Err(RemoteCError::CaptureError("Failed to get screen DC".to_string()));
        }
        
        // Get screen dimensions
        let (x, y, width, height) = if let Some(region) = self.config.region {
            (region.x, region.y, region.width as i32, region.height as i32)
        } else {
            let width = GetSystemMetrics(SM_CXSCREEN);
            let height = GetSystemMetrics(SM_CYSCREEN);
            (0, 0, width, height)
        };
        
        // Create a compatible DC
        let h_dc = CreateCompatibleDC(h_screen);
        if h_dc.is_null() {
            ReleaseDC(null_mut(), h_screen);
            return Err(RemoteCError::CaptureError("Failed to create compatible DC".to_string()));
        }
        
        // Create a compatible bitmap
        let h_bitmap = CreateCompatibleBitmap(h_screen, width, height);
        if h_bitmap.is_null() {
            DeleteDC(h_dc);
            ReleaseDC(null_mut(), h_screen);
            return Err(RemoteCError::CaptureError("Failed to create bitmap".to_string()));
        }
        
        // Select the bitmap into the DC
        let old_bitmap = SelectObject(h_dc, h_bitmap as *mut _);
        
        // BitBlt the screen data into our bitmap
        let success = BitBlt(
            h_dc,
            0,
            0,
            width,
            height,
            h_screen,
            x,
            y,
            SRCCOPY,
        );
        
        if success == 0 {
            SelectObject(h_dc, old_bitmap);
            DeleteObject(h_bitmap as *mut _);
            DeleteDC(h_dc);
            ReleaseDC(null_mut(), h_screen);
            return Err(RemoteCError::CaptureError("BitBlt failed".to_string()));
        }
        
        // Draw cursor if requested
        if self.config.capture_cursor {
            self.draw_cursor(h_dc, x, y);
        }
        
        // Get bitmap data
        let mut bmp_info = BITMAPINFO {
            bmiHeader: BITMAPINFOHEADER {
                biSize: std::mem::size_of::<BITMAPINFOHEADER>() as u32,
                biWidth: width,
                biHeight: -height, // Negative for top-down bitmap
                biPlanes: 1,
                biBitCount: 32,
                biCompression: BI_RGB,
                biSizeImage: 0,
                biXPelsPerMeter: 0,
                biYPelsPerMeter: 0,
                biClrUsed: 0,
                biClrImportant: 0,
            },
            bmiColors: [RGBQUAD {
                rgbBlue: 0,
                rgbGreen: 0,
                rgbRed: 0,
                rgbReserved: 0,
            }],
        };
        
        let data_size = (width * height * 4) as usize;
        let mut data = vec![0u8; data_size];
        
        let lines = GetDIBits(
            h_dc,
            h_bitmap,
            0,
            height as u32,
            data.as_mut_ptr() as *mut _,
            &mut bmp_info,
            DIB_RGB_COLORS,
        );
        
        // Cleanup
        SelectObject(h_dc, old_bitmap);
        DeleteObject(h_bitmap as *mut _);
        DeleteDC(h_dc);
        ReleaseDC(null_mut(), h_screen);
        
        if lines == 0 {
            return Err(RemoteCError::CaptureError("Failed to get bitmap data".to_string()));
        }
        
        Ok(ScreenFrame {
            width: width as u32,
            height: height as u32,
            data,
            timestamp: Instant::now(),
        })
    }
    
    #[cfg(target_os = "windows")]
    unsafe fn draw_cursor(&self, h_dc: HDC, offset_x: i32, offset_y: i32) {
        let mut cursor_info = CURSORINFO {
            cbSize: std::mem::size_of::<CURSORINFO>() as u32,
            flags: 0,
            hCursor: null_mut(),
            ptScreenPos: POINT { x: 0, y: 0 },
        };
        
        if GetCursorInfo(&mut cursor_info) != 0 && cursor_info.flags == CURSOR_SHOWING {
            let icon_info = &mut ICONINFO {
                fIcon: 0,
                xHotspot: 0,
                yHotspot: 0,
                hbmMask: null_mut(),
                hbmColor: null_mut(),
            };
            
            if GetIconInfo(cursor_info.hCursor, icon_info) != 0 {
                DrawIcon(
                    h_dc,
                    cursor_info.ptScreenPos.x - icon_info.xHotspot as i32 - offset_x,
                    cursor_info.ptScreenPos.y - icon_info.yHotspot as i32 - offset_y,
                    cursor_info.hCursor,
                );
                
                if !icon_info.hbmMask.is_null() {
                    DeleteObject(icon_info.hbmMask as *mut _);
                }
                if !icon_info.hbmColor.is_null() {
                    DeleteObject(icon_info.hbmColor as *mut _);
                }
            }
        }
    }
    
    fn capture_loop(
        active: Arc<AtomicBool>,
        frame_buffer: Arc<Mutex<Option<ScreenFrame>>>,
        config: CaptureConfig,
    ) {
        let frame_duration = Duration::from_micros(1_000_000 / config.target_fps as u64);
        
        while active.load(Ordering::Relaxed) {
            let frame_start = Instant::now();
            
            #[cfg(target_os = "windows")]
            {
                let capture = WindowsCapture {
                    config: config.clone(),
                    active: Arc::new(AtomicBool::new(true)),
                    capture_thread: None,
                    frame_buffer: Arc::new(Mutex::new(None)),
                };
                
                if let Ok(frame) = unsafe { capture.capture_screen() } {
                    if let Ok(mut buffer) = frame_buffer.lock() {
                        *buffer = Some(frame);
                    }
                }
            }
            
            let elapsed = frame_start.elapsed();
            if elapsed < frame_duration {
                thread::sleep(frame_duration - elapsed);
            }
        }
    }
}

impl ScreenCapture for WindowsCapture {
    fn start(&mut self) -> Result<()> {
        if self.active.load(Ordering::Relaxed) {
            return Err(RemoteCError::CaptureError("Already capturing".to_string()));
        }
        
        self.active.store(true, Ordering::Relaxed);
        
        let active = Arc::clone(&self.active);
        let frame_buffer = Arc::clone(&self.frame_buffer);
        let config = self.config.clone();
        
        self.capture_thread = Some(thread::spawn(move || {
            Self::capture_loop(active, frame_buffer, config);
        }));
        
        Ok(())
    }
    
    fn stop(&mut self) -> Result<()> {
        if !self.active.load(Ordering::Relaxed) {
            return Err(RemoteCError::CaptureError("Not capturing".to_string()));
        }
        
        self.active.store(false, Ordering::Relaxed);
        
        if let Some(thread) = self.capture_thread.take() {
            thread.join().map_err(|_| {
                RemoteCError::CaptureError("Failed to join capture thread".to_string())
            })?;
        }
        
        Ok(())
    }
    
    fn get_frame(&mut self) -> Result<Option<ScreenFrame>> {
        if !self.active.load(Ordering::Relaxed) {
            return Err(RemoteCError::CaptureError("Not capturing".to_string()));
        }
        
        Ok(self.frame_buffer.lock()
            .map_err(|_| RemoteCError::CaptureError("Failed to lock frame buffer".to_string()))?
            .take())
    }
    
    fn is_active(&self) -> bool {
        self.active.load(Ordering::Relaxed)
    }
    
    fn config(&self) -> &CaptureConfig {
        &self.config
    }
}

#[cfg(not(target_os = "windows"))]
impl WindowsCapture {
    pub fn new(_config: CaptureConfig) -> Result<Self> {
        Err(RemoteCError::UnsupportedPlatform(
            "Windows capture only available on Windows".to_string()
        ))
    }
}