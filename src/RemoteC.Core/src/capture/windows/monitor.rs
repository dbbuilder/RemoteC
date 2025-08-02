//! Windows-specific monitor enumeration

use crate::capture::monitor::{Monitor, MonitorBounds, MonitorOrientation};
use crate::{Result, RemoteCError};
use std::ffi::OsString;
use std::os::windows::ffi::OsStringExt;
use winapi::shared::minwindef::{BOOL, DWORD, LPARAM, TRUE};
use winapi::shared::windef::{HDC, HMONITOR, LPRECT, POINT};
use winapi::um::winuser::{
    EnumDisplayMonitors, GetMonitorInfoW, MonitorFromPoint, MONITORINFO, MONITORINFOEXW,
    MONITOR_DEFAULTTOPRIMARY,
};

/// Enumerate all monitors on Windows
pub fn enumerate_monitors_windows() -> Result<Vec<Monitor>> {
    let mut monitors = Vec::new();
    let monitors_ptr = &mut monitors as *mut Vec<Monitor> as LPARAM;

    unsafe {
        let result = EnumDisplayMonitors(
            std::ptr::null_mut(),
            std::ptr::null(),
            Some(monitor_enum_proc),
            monitors_ptr,
        );

        if result == 0 {
            return Err(RemoteCError::CaptureError(
                "Failed to enumerate monitors".to_string(),
            ));
        }
    }

    // Sort monitors by index to ensure consistent ordering
    monitors.sort_by_key(|m: &Monitor| m.index);

    Ok(monitors)
}

/// Callback function for EnumDisplayMonitors
unsafe extern "system" fn monitor_enum_proc(
    hmonitor: HMONITOR,
    _hdc: HDC,
    _lprect: LPRECT,
    lparam: LPARAM,
) -> BOOL {
    let monitors = &mut *(lparam as *mut Vec<Monitor>);
    
    // Get monitor info
    let mut monitor_info: MONITORINFOEXW = std::mem::zeroed();
    monitor_info.cbSize = std::mem::size_of::<MONITORINFOEXW>() as DWORD;

    if GetMonitorInfoW(hmonitor, &mut monitor_info as *mut MONITORINFOEXW as *mut MONITORINFO) == 0 {
        return TRUE; // Continue enumeration even if one monitor fails
    }

    // Convert device name from wide string
    let device_name_len = monitor_info
        .szDevice
        .iter()
        .position(|&c| c == 0)
        .unwrap_or(monitor_info.szDevice.len());
    let device_name = OsString::from_wide(&monitor_info.szDevice[..device_name_len])
        .to_string_lossy()
        .to_string();

    // Extract monitor bounds
    let bounds = MonitorBounds {
        x: monitor_info.rcMonitor.left,
        y: monitor_info.rcMonitor.top,
        width: (monitor_info.rcMonitor.right - monitor_info.rcMonitor.left) as u32,
        height: (monitor_info.rcMonitor.bottom - monitor_info.rcMonitor.top) as u32,
    };

    let work_area = MonitorBounds {
        x: monitor_info.rcWork.left,
        y: monitor_info.rcWork.top,
        width: (monitor_info.rcWork.right - monitor_info.rcWork.left) as u32,
        height: (monitor_info.rcWork.bottom - monitor_info.rcWork.top) as u32,
    };

    // Check if this is the primary monitor
    let is_primary = (monitor_info.dwFlags & 0x00000001) != 0; // MONITORINFOF_PRIMARY

    // Get DPI scale factor
    let scale_factor = get_monitor_scale_factor(hmonitor);

    // Create monitor info
    let monitor = Monitor {
        id: device_name.clone(),
        index: monitors.len(),
        name: format!("Display {}", monitors.len() + 1),
        is_primary,
        bounds,
        work_area,
        scale_factor,
        refresh_rate: 60, // Default, would need additional API calls to get actual rate
        bit_depth: 32,    // Default, would need additional API calls
        orientation: MonitorOrientation::Landscape, // Default
    };

    monitors.push(monitor);
    TRUE
}

/// Get the DPI scale factor for a monitor
fn get_monitor_scale_factor(hmonitor: HMONITOR) -> f32 {
    // Try to use newer DPI APIs if available
    #[cfg(windows)]
    {
        use winapi::um::shellscalingapi::{GetDpiForMonitor, MDT_EFFECTIVE_DPI};
        use winapi::shared::winerror::S_OK;
        
        let mut dpi_x: u32 = 0;
        let mut dpi_y: u32 = 0;
        
        unsafe {
            let result = GetDpiForMonitor(hmonitor, MDT_EFFECTIVE_DPI, &mut dpi_x, &mut dpi_y);
            if result == S_OK && dpi_x > 0 {
                return dpi_x as f32 / 96.0; // 96 DPI is 100% scaling
            }
        }
    }
    
    // Fallback to 100% scaling
    1.0
}

/// Get monitor at a specific point
pub fn get_monitor_at_point_windows(x: i32, y: i32) -> Result<Monitor> {
    let point = POINT { x, y };
    
    unsafe {
        let hmonitor = MonitorFromPoint(point, MONITOR_DEFAULTTOPRIMARY);
        if hmonitor.is_null() {
            return Err(RemoteCError::CaptureError(
                "No monitor found at point".to_string(),
            ));
        }
        
        // Get monitor info
        let mut monitor_info: MONITORINFOEXW = std::mem::zeroed();
        monitor_info.cbSize = std::mem::size_of::<MONITORINFOEXW>() as DWORD;

        if GetMonitorInfoW(hmonitor, &mut monitor_info as *mut MONITORINFOEXW as *mut MONITORINFO) == 0 {
            return Err(RemoteCError::CaptureError(
                "Failed to get monitor info".to_string(),
            ));
        }
        
        // Convert to our Monitor struct
        let device_name_len = monitor_info
            .szDevice
            .iter()
            .position(|&c| c == 0)
            .unwrap_or(monitor_info.szDevice.len());
        let device_name = OsString::from_wide(&monitor_info.szDevice[..device_name_len])
            .to_string_lossy()
            .to_string();
            
        let bounds = MonitorBounds {
            x: monitor_info.rcMonitor.left,
            y: monitor_info.rcMonitor.top,
            width: (monitor_info.rcMonitor.right - monitor_info.rcMonitor.left) as u32,
            height: (monitor_info.rcMonitor.bottom - monitor_info.rcMonitor.top) as u32,
        };

        let work_area = MonitorBounds {
            x: monitor_info.rcWork.left,
            y: monitor_info.rcWork.top,
            width: (monitor_info.rcWork.right - monitor_info.rcWork.left) as u32,
            height: (monitor_info.rcWork.bottom - monitor_info.rcWork.top) as u32,
        };
        
        let is_primary = (monitor_info.dwFlags & 0x00000001) != 0;
        let scale_factor = get_monitor_scale_factor(hmonitor);
        
        Ok(Monitor {
            id: device_name,
            index: 0, // Would need to enumerate all to get proper index
            name: "Monitor".to_string(),
            is_primary,
            bounds,
            work_area,
            scale_factor,
            refresh_rate: 60,
            bit_depth: 32,
            orientation: MonitorOrientation::Landscape,
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_enumerate_monitors() {
        // This test requires a display to be connected
        if std::env::var("CI").is_err() {
            let monitors = enumerate_monitors_windows();
            assert!(monitors.is_ok());
            
            let monitors = monitors.unwrap();
            assert!(!monitors.is_empty());
            
            // Should have at least one primary monitor
            assert!(monitors.iter().any(|m| m.is_primary));
        }
    }
}