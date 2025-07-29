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
    match crate::capture::create_capture() {
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
            (*frame_data).timestamp = frame.timestamp;
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