//! Windows-specific screen capture implementation

use super::{CaptureConfig, ScreenCapture, ScreenFrame, CaptureMode};
use super::monitor::{Monitor, MonitorBounds, MonitorOrientation, VirtualDesktop};
use crate::{Result, RemoteCError};
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::{Duration, Instant};
use std::ffi::OsString;
use std::os::windows::ffi::OsStringExt;

#[cfg(target_os = "windows")]
use winapi::{
    shared::windef::{HDC, HBITMAP, RECT, HMONITOR},
    shared::minwindef::{BOOL, LPARAM, DWORD},
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
        
        // Get virtual desktop info
        let desktop = super::monitor::get_virtual_desktop()?;
        
        // Determine capture bounds based on mode
        let (x, y, width, height) = match &self.config.mode {
            CaptureMode::SingleMonitor(idx) => {
                let monitor = desktop.get_monitor(*idx)
                    .ok_or_else(|| RemoteCError::CaptureError(format!("Monitor {} not found", idx)))?;
                (monitor.bounds.x, monitor.bounds.y, 
                 monitor.bounds.width as i32, monitor.bounds.height as i32)
            },
            CaptureMode::PrimaryMonitor => {
                let monitor = desktop.primary_monitor();
                (monitor.bounds.x, monitor.bounds.y, 
                 monitor.bounds.width as i32, monitor.bounds.height as i32)
            },
            CaptureMode::AllMonitors => {
                (desktop.total_bounds.x, desktop.total_bounds.y,
                 desktop.total_bounds.width as i32, desktop.total_bounds.height as i32)
            },
            CaptureMode::SelectedMonitors(indices) => {
                // Calculate combined bounds of selected monitors
                let mut combined_bounds = None;
                for idx in indices {
                    if let Some(monitor) = desktop.get_monitor(*idx) {
                        match combined_bounds {
                            None => combined_bounds = Some(monitor.bounds),
                            Some(ref mut bounds) => *bounds = bounds.union(&monitor.bounds),
                        }
                    }
                }
                let bounds = combined_bounds
                    .ok_or_else(|| RemoteCError::CaptureError("No valid monitors selected".to_string()))?;
                (bounds.x, bounds.y, bounds.width as i32, bounds.height as i32)
            },
            CaptureMode::WindowMonitor(_) => {
                // For now, capture primary monitor
                let monitor = desktop.primary_monitor();
                (monitor.bounds.x, monitor.bounds.y, 
                 monitor.bounds.width as i32, monitor.bounds.height as i32)
            },
        };
        
        // Apply region if specified
        let (x, y, width, height) = if let Some(region) = self.config.region {
            (x + region.x, y + region.y, 
             region.width.min(width as u32) as i32, 
             region.height.min(height as u32) as i32)
        } else {
            (x, y, width, height)
        };
        
        // Get the device context of the screen
        let h_screen = GetDC(null_mut());
        if h_screen.is_null() {
            return Err(RemoteCError::CaptureError("Failed to get screen DC".to_string()));
        }
        
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

/// Monitor enumeration for Windows
#[cfg(target_os = "windows")]
pub fn enumerate_monitors_windows() -> Result<Vec<Monitor>> {
    use std::ptr::null_mut;
    use std::collections::HashMap;
    
    // Structure to hold monitor info during enumeration
    struct MonitorInfo {
        handle: HMONITOR,
        rect: RECT,
        work_rect: RECT,
        is_primary: bool,
        device_name: String,
    }
    
    let mut monitors_info: Vec<MonitorInfo> = Vec::new();
    
    // Enumerate all monitors
    unsafe extern "system" fn enum_monitors_proc(
        h_monitor: HMONITOR,
        _hdc_monitor: HDC,
        _lprc_monitor: *mut RECT,
        lp_param: LPARAM,
    ) -> BOOL {
        let monitors = &mut *(lp_param as *mut Vec<MonitorInfo>);
        
        let mut info = MONITORINFOEXW {
            cbSize: std::mem::size_of::<MONITORINFOEXW>() as DWORD,
            rcMonitor: RECT { left: 0, top: 0, right: 0, bottom: 0 },
            rcWork: RECT { left: 0, top: 0, right: 0, bottom: 0 },
            dwFlags: 0,
            szDevice: [0; 32],
        };
        
        if GetMonitorInfoW(h_monitor, &mut info as *mut _ as *mut MONITORINFO) != 0 {
            // Convert device name from wide string
            let device_name_slice = &info.szDevice[..];
            let null_pos = device_name_slice.iter().position(|&c| c == 0).unwrap_or(32);
            let device_name = OsString::from_wide(&device_name_slice[..null_pos])
                .to_string_lossy()
                .to_string();
            
            monitors.push(MonitorInfo {
                handle: h_monitor,
                rect: info.rcMonitor,
                work_rect: info.rcWork,
                is_primary: (info.dwFlags & MONITORINFOF_PRIMARY) != 0,
                device_name,
            });
        }
        
        1 // Continue enumeration
    }
    
    unsafe {
        EnumDisplayMonitors(
            null_mut(),
            null_mut(),
            Some(enum_monitors_proc),
            &mut monitors_info as *mut _ as LPARAM,
        );
    }
    
    // Convert to our Monitor structure
    let mut monitors = Vec::new();
    for (index, info) in monitors_info.iter().enumerate() {
        // Get display settings for this monitor
        let mut dm = DEVMODEW {
            dmSize: std::mem::size_of::<DEVMODEW>() as u16,
            ..std::mem::zeroed()
        };
        
        let mut device_name_wide = [0u16; 32];
        let device_name_slice = info.device_name.encode_utf16().collect::<Vec<u16>>();
        let copy_len = device_name_slice.len().min(31);
        device_name_wide[..copy_len].copy_from_slice(&device_name_slice[..copy_len]);
        
        let (refresh_rate, bit_depth, orientation) = unsafe {
            if EnumDisplaySettingsW(
                device_name_wide.as_ptr(),
                ENUM_CURRENT_SETTINGS,
                &mut dm,
            ) != 0 {
                (
                    dm.dmDisplayFrequency,
                    dm.dmBitsPerPel,
                    match dm.dmDisplayOrientation {
                        DMDO_90 => MonitorOrientation::Portrait,
                        DMDO_180 => MonitorOrientation::LandscapeFlipped,
                        DMDO_270 => MonitorOrientation::PortraitFlipped,
                        _ => MonitorOrientation::Landscape,
                    },
                )
            } else {
                (60, 32, MonitorOrientation::Landscape)
            }
        };
        
        // Get DPI for scale factor
        let scale_factor = unsafe {
            let dpi_x = GetDeviceCaps(GetDC(null_mut()), LOGPIXELSX);
            dpi_x as f32 / 96.0
        };
        
        monitors.push(Monitor {
            id: format!("\\\\?\\DISPLAY#{}", index + 1),
            index,
            name: info.device_name.clone(),
            is_primary: info.is_primary,
            bounds: MonitorBounds {
                x: info.rect.left,
                y: info.rect.top,
                width: (info.rect.right - info.rect.left) as u32,
                height: (info.rect.bottom - info.rect.top) as u32,
            },
            work_area: MonitorBounds {
                x: info.work_rect.left,
                y: info.work_rect.top,
                width: (info.work_rect.right - info.work_rect.left) as u32,
                height: (info.work_rect.bottom - info.work_rect.top) as u32,
            },
            scale_factor,
            refresh_rate: refresh_rate as u32,
            bit_depth: bit_depth as u32,
            orientation,
        });
    }
    
    // Sort by position to ensure consistent ordering
    monitors.sort_by(|a, b| {
        match a.bounds.y.cmp(&b.bounds.y) {
            std::cmp::Ordering::Equal => a.bounds.x.cmp(&b.bounds.x),
            other => other,
        }
    });
    
    // Update indices after sorting
    for (i, monitor) in monitors.iter_mut().enumerate() {
        monitor.index = i;
    }
    
    if monitors.is_empty() {
        Err(RemoteCError::CaptureError("No monitors found".to_string()))
    } else {
        Ok(monitors)
    }
}

#[cfg(not(target_os = "windows"))]
pub fn enumerate_monitors_windows() -> Result<Vec<Monitor>> {
    Err(RemoteCError::UnsupportedPlatform(
        "Windows monitor enumeration only available on Windows".to_string()
    ))
}