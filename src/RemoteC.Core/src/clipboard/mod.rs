use std::error::Error;
use std::fmt;
use std::time::SystemTime;

pub mod windows;

#[cfg(target_os = "windows")]
pub use windows::WindowsClipboard as PlatformClipboard;

#[cfg(target_os = "linux")]
pub mod linux;
#[cfg(target_os = "linux")]
pub use linux::LinuxClipboard as PlatformClipboard;

#[cfg(target_os = "macos")]
pub mod macos;
#[cfg(target_os = "macos")]
pub use macos::MacOSClipboard as PlatformClipboard;

/// Clipboard content types
#[derive(Debug, Clone, PartialEq)]
pub enum ClipboardContentType {
    Text,
    Html,
    RichText,
    Image,
    FileList,
    Custom,
}

/// Clipboard formats
#[derive(Debug, Clone, PartialEq)]
pub enum ClipboardFormat {
    Text,
    Html,
    RichText,
    Image,
    FileList,
}

/// Clipboard content
#[derive(Debug, Clone, PartialEq)]
pub struct ClipboardContent {
    pub content_type: ClipboardContentType,
    pub text: Option<String>,
    pub html: Option<String>,
    pub image_data: Option<Vec<u8>>,
    pub image_format: Option<String>,
    pub files: Option<Vec<String>>,
    pub size: usize,
    pub timestamp: SystemTime,
}

/// Clipboard errors
#[derive(Debug)]
pub enum ClipboardError {
    NotAvailable,
    AccessDenied,
    InvalidFormat,
    ContentTooLarge,
    PlatformError(String),
    NotImplemented(String),
}

impl fmt::Display for ClipboardError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            ClipboardError::NotAvailable => write!(f, "Clipboard not available"),
            ClipboardError::AccessDenied => write!(f, "Clipboard access denied"),
            ClipboardError::InvalidFormat => write!(f, "Invalid clipboard format"),
            ClipboardError::ContentTooLarge => write!(f, "Clipboard content too large"),
            ClipboardError::PlatformError(msg) => write!(f, "Platform error: {}", msg),
            ClipboardError::NotImplemented(msg) => write!(f, "Not implemented: {}", msg),
        }
    }
}

impl Error for ClipboardError {}

/// Clipboard trait for platform-specific implementations
pub trait Clipboard {
    /// Get clipboard content
    fn get_content(&self) -> Result<Option<ClipboardContent>, ClipboardError>;
    
    /// Set clipboard content
    fn set_content(&self, content: ClipboardContent) -> Result<(), ClipboardError>;
    
    /// Clear clipboard
    fn clear(&self) -> Result<(), ClipboardError>;
    
    /// Check if format is supported
    fn is_format_supported(&self, format: &ClipboardFormat) -> bool;
    
    /// Start monitoring clipboard changes
    fn start_monitoring(&mut self, callback: Box<dyn Fn(ClipboardContent) + Send + 'static>) -> Result<(), ClipboardError>;
    
    /// Stop monitoring clipboard changes
    fn stop_monitoring(&mut self) -> Result<(), ClipboardError>;
}

/// Create platform-specific clipboard instance
pub fn create_clipboard() -> Result<Box<dyn Clipboard>, ClipboardError> {
    Ok(Box::new(PlatformClipboard::new()?))
}

/// FFI-safe clipboard content representation
#[repr(C)]
pub struct ClipboardContentFFI {
    pub content_type: u32,
    pub text: *const u8,
    pub text_len: usize,
    pub html: *const u8,
    pub html_len: usize,
    pub image_data: *const u8,
    pub image_data_len: usize,
    pub image_format: *const u8,
    pub image_format_len: usize,
    pub files: *const *const u8,
    pub file_lengths: *const usize,
    pub file_count: usize,
    pub size: usize,
    pub timestamp: u64,
}

impl ClipboardContentFFI {
    pub fn from_content(content: &ClipboardContent) -> Self {
        let content_type = match content.content_type {
            ClipboardContentType::Text => 0,
            ClipboardContentType::Html => 1,
            ClipboardContentType::RichText => 2,
            ClipboardContentType::Image => 3,
            ClipboardContentType::FileList => 4,
            ClipboardContentType::Custom => 5,
        };

        let (text, text_len) = if let Some(ref t) = content.text {
            (t.as_ptr(), t.len())
        } else {
            (std::ptr::null(), 0)
        };

        let (html, html_len) = if let Some(ref h) = content.html {
            (h.as_ptr(), h.len())
        } else {
            (std::ptr::null(), 0)
        };

        let (image_data, image_data_len) = if let Some(ref data) = content.image_data {
            (data.as_ptr(), data.len())
        } else {
            (std::ptr::null(), 0)
        };

        let (image_format, image_format_len) = if let Some(ref fmt) = content.image_format {
            (fmt.as_ptr(), fmt.len())
        } else {
            (std::ptr::null(), 0)
        };

        let (files, file_lengths, file_count) = if let Some(ref f) = content.files {
            let file_ptrs: Vec<*const u8> = f.iter().map(|s| s.as_ptr()).collect();
            let lengths: Vec<usize> = f.iter().map(|s| s.len()).collect();
            (file_ptrs.as_ptr(), lengths.as_ptr(), f.len())
        } else {
            (std::ptr::null(), std::ptr::null(), 0)
        };

        let timestamp = content.timestamp
            .duration_since(SystemTime::UNIX_EPOCH)
            .unwrap_or_default()
            .as_secs();

        ClipboardContentFFI {
            content_type,
            text,
            text_len,
            html,
            html_len,
            image_data,
            image_data_len,
            image_format,
            image_format_len,
            files,
            file_lengths,
            file_count,
            size: content.size,
            timestamp,
        }
    }
}

/// FFI functions
#[no_mangle]
pub extern "C" fn remotec_clipboard_get_content() -> *mut ClipboardContentFFI {
    match create_clipboard() {
        Ok(clipboard) => {
            match clipboard.get_content() {
                Ok(Some(content)) => {
                    let ffi_content = ClipboardContentFFI::from_content(&content);
                    Box::into_raw(Box::new(ffi_content))
                }
                _ => std::ptr::null_mut()
            }
        }
        _ => std::ptr::null_mut()
    }
}

#[no_mangle]
pub extern "C" fn remotec_clipboard_set_text(text: *const u8, len: usize) -> i32 {
    if text.is_null() || len == 0 {
        return -1;
    }

    let text_slice = unsafe { std::slice::from_raw_parts(text, len) };
    let text_str = match std::str::from_utf8(text_slice) {
        Ok(s) => s.to_string(),
        Err(_) => return -1,
    };

    let content = ClipboardContent {
        content_type: ClipboardContentType::Text,
        text: Some(text_str),
        html: None,
        image_data: None,
        image_format: None,
        files: None,
        size: len,
        timestamp: SystemTime::now(),
    };

    match create_clipboard() {
        Ok(clipboard) => {
            match clipboard.set_content(content) {
                Ok(_) => 0,
                Err(_) => -1,
            }
        }
        Err(_) => -1,
    }
}

#[no_mangle]
pub extern "C" fn remotec_clipboard_set_image(data: *const u8, len: usize, format: *const u8, format_len: usize) -> i32 {
    if data.is_null() || len == 0 {
        return -1;
    }

    let image_data = unsafe { std::slice::from_raw_parts(data, len) }.to_vec();
    
    let image_format = if !format.is_null() && format_len > 0 {
        let format_slice = unsafe { std::slice::from_raw_parts(format, format_len) };
        std::str::from_utf8(format_slice).ok().map(|s| s.to_string())
    } else {
        Some("PNG".to_string())
    };

    let content = ClipboardContent {
        content_type: ClipboardContentType::Image,
        text: None,
        html: None,
        image_data: Some(image_data),
        image_format,
        files: None,
        size: len,
        timestamp: SystemTime::now(),
    };

    match create_clipboard() {
        Ok(clipboard) => {
            match clipboard.set_content(content) {
                Ok(_) => 0,
                Err(_) => -1,
            }
        }
        Err(_) => -1,
    }
}

#[no_mangle]
pub extern "C" fn remotec_clipboard_clear() -> i32 {
    match create_clipboard() {
        Ok(clipboard) => {
            match clipboard.clear() {
                Ok(_) => 0,
                Err(_) => -1,
            }
        }
        Err(_) => -1,
    }
}

#[no_mangle]
pub extern "C" fn remotec_clipboard_free_content(content: *mut ClipboardContentFFI) {
    if !content.is_null() {
        unsafe {
            let _ = Box::from_raw(content);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_clipboard_content_type() {
        let content = ClipboardContent {
            content_type: ClipboardContentType::Text,
            text: Some("Hello".to_string()),
            html: None,
            image_data: None,
            image_format: None,
            files: None,
            size: 5,
            timestamp: SystemTime::now(),
        };

        assert_eq!(content.content_type, ClipboardContentType::Text);
        assert_eq!(content.text, Some("Hello".to_string()));
        assert_eq!(content.size, 5);
    }
}