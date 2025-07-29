//! FFI module for .NET interop

use crate::{Result, RemoteCError};
use std::ffi::{c_char, CStr, CString};
use std::ptr;
use std::sync::Arc;
use tokio::runtime::Runtime;
use once_cell::sync::Lazy;

/// Global runtime for async operations
static RUNTIME: Lazy<Runtime> = Lazy::new(|| {
    Runtime::new().expect("Failed to create Tokio runtime")
});

/// Opaque handle for RemoteC session
#[repr(C)]
pub struct RemoteCHandle {
    _private: [u8; 0],
}

/// Screen capture handle
#[repr(C)]
pub struct CaptureHandle {
    _private: [u8; 0],
}

/// Input simulator handle
#[repr(C)]
pub struct InputHandle {
    _private: [u8; 0],
}

/// Transport handle
#[repr(C)]
pub struct TransportHandle {
    _private: [u8; 0],
}

/// Video encoder handle
#[repr(C)]
pub struct EncoderHandle {
    _private: [u8; 0],
}

/// FFI-safe frame data
#[repr(C)]
pub struct FrameData {
    pub width: u32,
    pub height: u32,
    pub data: *const u8,
    pub data_len: usize,
    pub timestamp: u64,
}

/// FFI-safe input event
#[repr(C)]
pub struct InputEventFFI {
    pub event_type: u32,
    pub x: i32,
    pub y: i32,
    pub button: u32,
    pub key_code: u32,
    pub modifiers: u32,
}

/// Initialize the RemoteC Core library
#[no_mangle]
pub extern "C" fn remotec_init() -> i32 {
    match crate::initialize() {
        Ok(_) => 0,
        Err(_) => -1,
    }
}

/// Get the version string
#[no_mangle]
pub extern "C" fn remotec_version() -> *const c_char {
    let version = CString::new(crate::version()).unwrap();
    version.into_raw()
}

/// Free a string returned by the library
#[no_mangle]
pub unsafe extern "C" fn remotec_free_string(s: *mut c_char) {
    if !s.is_null() {
        let _ = CString::from_raw(s);
    }
}

/// Create a screen capture instance
#[no_mangle]
pub extern "C" fn remotec_capture_create() -> *mut CaptureHandle {
    match crate::capture::create_capture(crate::capture::CaptureConfig::default()) {
        Ok(capture) => {
            let boxed = Box::new(capture);
            Box::into_raw(boxed) as *mut CaptureHandle
        }
        Err(_) => ptr::null_mut(),
    }
}

/// Start screen capture
#[no_mangle]
pub unsafe extern "C" fn remotec_capture_start(handle: *mut CaptureHandle) -> i32 {
    if handle.is_null() {
        log::error!("remotec_capture_start: null handle");
        return -1;
    }
    
    let capture = &mut *(handle as *mut Box<dyn crate::capture::ScreenCapture>);
    match capture.start() {
        Ok(_) => {
            log::info!("Screen capture started successfully");
            0
        }
        Err(e) => {
            log::error!("Failed to start screen capture: {}", e);
            -1
        }
    }
}

/// Get a frame from screen capture
#[no_mangle]
pub unsafe extern "C" fn remotec_capture_get_frame(
    handle: *mut CaptureHandle,
    frame_data: *mut FrameData,
) -> i32 {
    if handle.is_null() || frame_data.is_null() {
        return -1;
    }
    
    let capture = &mut *(handle as *mut Box<dyn crate::capture::ScreenCapture>);
    match capture.get_frame() {
        Ok(Some(frame)) => {
            (*frame_data).width = frame.width;
            (*frame_data).height = frame.height;
            (*frame_data).data = frame.data.as_ptr();
            (*frame_data).data_len = frame.data.len();
            (*frame_data).timestamp = frame.timestamp.elapsed().as_micros() as u64;
            0
        }
        Ok(None) => 1, // No frame available
        Err(_) => -1,
    }
}

/// Stop screen capture
#[no_mangle]
pub unsafe extern "C" fn remotec_capture_stop(handle: *mut CaptureHandle) -> i32 {
    if handle.is_null() {
        return -1;
    }
    
    let capture = &mut *(handle as *mut Box<dyn crate::capture::ScreenCapture>);
    match capture.stop() {
        Ok(_) => 0,
        Err(_) => -1,
    }
}

/// Destroy screen capture instance
#[no_mangle]
pub unsafe extern "C" fn remotec_capture_destroy(handle: *mut CaptureHandle) {
    if !handle.is_null() {
        let _ = Box::from_raw(handle as *mut Box<dyn crate::capture::ScreenCapture>);
    }
}

/// Create an input simulator instance
#[no_mangle]
pub extern "C" fn remotec_input_create() -> *mut InputHandle {
    match crate::input::create_simulator() {
        Ok(simulator) => {
            let boxed = Box::new(simulator);
            Box::into_raw(boxed) as *mut InputHandle
        }
        Err(_) => ptr::null_mut(),
    }
}

/// Send mouse event
#[no_mangle]
pub unsafe extern "C" fn remotec_input_mouse_move(
    handle: *mut InputHandle,
    x: i32,
    y: i32,
) -> i32 {
    if handle.is_null() {
        return -1;
    }
    
    let simulator = &mut *(handle as *mut Box<dyn crate::input::InputSimulator>);
    match simulator.mouse_event(crate::input::MouseEvent::Move { x, y }) {
        Ok(_) => 0,
        Err(_) => -1,
    }
}

/// Send mouse click
#[no_mangle]
pub unsafe extern "C" fn remotec_input_mouse_click(
    handle: *mut InputHandle,
    button: u32,
) -> i32 {
    if handle.is_null() {
        return -1;
    }
    
    let button = match button {
        0 => crate::input::MouseButton::Left,
        1 => crate::input::MouseButton::Right,
        2 => crate::input::MouseButton::Middle,
        _ => return -1,
    };
    
    let simulator = &mut *(handle as *mut Box<dyn crate::input::InputSimulator>);
    match simulator.mouse_event(crate::input::MouseEvent::Click { button }) {
        Ok(_) => 0,
        Err(_) => -1,
    }
}

/// Send key press
#[no_mangle]
pub unsafe extern "C" fn remotec_input_key_press(
    handle: *mut InputHandle,
    key_code: u32,
) -> i32 {
    if handle.is_null() {
        return -1;
    }
    
    // Map key code to KeyCode enum (simplified)
    let code = match key_code {
        65 => crate::input::KeyCode::A,
        66 => crate::input::KeyCode::B,
        // ... add more mappings
        _ => return -1,
    };
    
    let simulator = &mut *(handle as *mut Box<dyn crate::input::InputSimulator>);
    match simulator.keyboard_event(crate::input::KeyboardEvent::KeyPress { code }) {
        Ok(_) => 0,
        Err(_) => -1,
    }
}

/// Destroy input simulator instance
#[no_mangle]
pub unsafe extern "C" fn remotec_input_destroy(handle: *mut InputHandle) {
    if !handle.is_null() {
        let _ = Box::from_raw(handle as *mut Box<dyn crate::input::InputSimulator>);
    }
}

/// Create a transport instance
#[no_mangle]
pub extern "C" fn remotec_transport_create(protocol: u32) -> *mut TransportHandle {
    let protocol = match protocol {
        0 => crate::transport::TransportProtocol::Quic,
        1 => crate::transport::TransportProtocol::WebRtcData,
        2 => crate::transport::TransportProtocol::Udp,
        _ => return ptr::null_mut(),
    };
    
    let config = crate::transport::TransportConfig {
        protocol,
        ..Default::default()
    };
    
    match RUNTIME.block_on(crate::transport::create_transport(config)) {
        Ok(transport) => {
            let boxed = Box::new(transport);
            Box::into_raw(boxed) as *mut TransportHandle
        }
        Err(_) => ptr::null_mut(),
    }
}

/// Destroy transport instance
#[no_mangle]
pub unsafe extern "C" fn remotec_transport_destroy(handle: *mut TransportHandle) {
    if !handle.is_null() {
        let _ = Box::from_raw(handle as *mut Box<dyn crate::transport::Transport>);
    }
}

/// Monitor info structure for FFI
#[repr(C)]
pub struct MonitorInfoFFI {
    pub id: *const c_char,
    pub index: u32,
    pub name: *const c_char,
    pub is_primary: u8,
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
    pub work_x: i32,
    pub work_y: i32,
    pub work_width: u32,
    pub work_height: u32,
    pub scale_factor: f32,
    pub refresh_rate: u32,
    pub bit_depth: u32,
    pub orientation: u32,
}

/// Enumerate all available monitors
#[no_mangle]
pub extern "C" fn remotec_enumerate_monitors(
    monitors: *mut MonitorInfoFFI,
    count: *mut u32,
) -> i32 {
    if count.is_null() {
        return -1;
    }
    
    match crate::capture::monitor::enumerate_monitors() {
        Ok(monitor_list) => {
            unsafe {
                *count = monitor_list.len() as u32;
                
                if !monitors.is_null() {
                    for (i, monitor) in monitor_list.iter().enumerate() {
                        let ffi_monitor = monitors.add(i);
                        
                        // Convert strings to C strings
                        let id = CString::new(monitor.id.clone()).unwrap();
                        let name = CString::new(monitor.name.clone()).unwrap();
                        
                        (*ffi_monitor).id = id.into_raw();
                        (*ffi_monitor).index = monitor.index as u32;
                        (*ffi_monitor).name = name.into_raw();
                        (*ffi_monitor).is_primary = if monitor.is_primary { 1 } else { 0 };
                        (*ffi_monitor).x = monitor.bounds.x;
                        (*ffi_monitor).y = monitor.bounds.y;
                        (*ffi_monitor).width = monitor.bounds.width;
                        (*ffi_monitor).height = monitor.bounds.height;
                        (*ffi_monitor).work_x = monitor.work_area.x;
                        (*ffi_monitor).work_y = monitor.work_area.y;
                        (*ffi_monitor).work_width = monitor.work_area.width;
                        (*ffi_monitor).work_height = monitor.work_area.height;
                        (*ffi_monitor).scale_factor = monitor.scale_factor;
                        (*ffi_monitor).refresh_rate = monitor.refresh_rate;
                        (*ffi_monitor).bit_depth = monitor.bit_depth;
                        (*ffi_monitor).orientation = monitor.orientation as u32;
                    }
                }
            }
            0
        }
        Err(e) => {
            log::error!("Failed to enumerate monitors: {}", e);
            -1
        }
    }
}

/// Free monitor info strings
#[no_mangle]
pub unsafe extern "C" fn remotec_free_monitor_info(
    monitors: *mut MonitorInfoFFI,
    count: u32,
) {
    if !monitors.is_null() {
        for i in 0..count {
            let monitor = monitors.add(i as usize);
            
            if !(*monitor).id.is_null() {
                let _ = CString::from_raw((*monitor).id as *mut c_char);
            }
            if !(*monitor).name.is_null() {
                let _ = CString::from_raw((*monitor).name as *mut c_char);
            }
        }
    }
}

/// Get virtual desktop information as JSON
#[no_mangle]
pub extern "C" fn remotec_get_virtual_desktop() -> *mut c_char {
    match crate::capture::monitor::get_virtual_desktop() {
        Ok(desktop) => {
            // Convert to JSON for easy .NET deserialization
            let json = serde_json::json!({
                "monitors": desktop.monitors.iter().map(|m| {
                    serde_json::json!({
                        "id": m.id,
                        "index": m.index,
                        "name": m.name,
                        "isPrimary": m.is_primary,
                        "bounds": {
                            "x": m.bounds.x,
                            "y": m.bounds.y,
                            "width": m.bounds.width,
                            "height": m.bounds.height,
                        },
                        "workArea": {
                            "x": m.work_area.x,
                            "y": m.work_area.y,
                            "width": m.work_area.width,
                            "height": m.work_area.height,
                        },
                        "scaleFactor": m.scale_factor,
                        "refreshRate": m.refresh_rate,
                        "bitDepth": m.bit_depth,
                        "orientation": m.orientation as u32,
                    })
                }).collect::<Vec<_>>(),
                "totalBounds": {
                    "x": desktop.total_bounds.x,
                    "y": desktop.total_bounds.y,
                    "width": desktop.total_bounds.width,
                    "height": desktop.total_bounds.height,
                },
                "primaryIndex": desktop.primary_index,
            });
            
            match CString::new(json.to_string()) {
                Ok(cstr) => cstr.into_raw(),
                Err(_) => ptr::null_mut(),
            }
        }
        Err(e) => {
            log::error!("Failed to get virtual desktop: {}", e);
            ptr::null_mut()
        }
    }
}

/// Create capture with specific monitor configuration
#[no_mangle]
pub extern "C" fn remotec_capture_create_with_config(
    mode: u32,
    monitor_indices: *const u32,
    monitor_count: u32,
    target_fps: u32,
    capture_cursor: u8,
) -> *mut CaptureHandle {
    use crate::capture::{CaptureConfig, CaptureMode, CaptureQuality};
    
    let mode = match mode {
        0 => CaptureMode::PrimaryMonitor,
        1 => {
            if !monitor_indices.is_null() && monitor_count > 0 {
                CaptureMode::SingleMonitor(unsafe { *monitor_indices } as usize)
            } else {
                CaptureMode::PrimaryMonitor
            }
        }
        2 => CaptureMode::AllMonitors,
        3 => {
            if !monitor_indices.is_null() && monitor_count > 0 {
                let indices = unsafe {
                    std::slice::from_raw_parts(monitor_indices, monitor_count as usize)
                        .iter()
                        .map(|&idx| idx as usize)
                        .collect()
                };
                CaptureMode::SelectedMonitors(indices)
            } else {
                CaptureMode::AllMonitors
            }
        }
        _ => CaptureMode::PrimaryMonitor,
    };
    
    let config = CaptureConfig {
        mode,
        target_fps,
        capture_cursor: capture_cursor != 0,
        region: None,
        quality: CaptureQuality::default(),
    };
    
    match crate::capture::create_capture(config) {
        Ok(capture) => {
            let boxed = Box::new(capture);
            Box::into_raw(boxed) as *mut CaptureHandle
        }
        Err(e) => {
            log::error!("Failed to create capture: {}", e);
            ptr::null_mut()
        }
    }
}