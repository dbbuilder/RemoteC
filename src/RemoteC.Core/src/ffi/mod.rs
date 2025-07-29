//! FFI module for .NET interop

use crate::{capture::CaptureConfig, Result};
use std::ffi::{c_char, CStr, CString};
use std::ptr;

#[repr(C)]
pub struct RemoteCHandle {
    _private: [u8; 0],
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