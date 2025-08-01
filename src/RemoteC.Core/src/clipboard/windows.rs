use super::{Clipboard, ClipboardContent, ClipboardContentType, ClipboardError, ClipboardFormat};
use std::ptr;
use std::time::SystemTime;
use winapi::ctypes::c_void;
use winapi::shared::minwindef::{FALSE, UINT};
use winapi::shared::windef::HWND;
use winapi::um::winbase::{GlobalAlloc, GlobalFree, GlobalLock, GlobalSize, GlobalUnlock, GMEM_MOVEABLE};
use winapi::um::winuser::{
    CloseClipboard, EmptyClipboard, GetClipboardData, IsClipboardFormatAvailable,
    OpenClipboard, RegisterClipboardFormatW, SetClipboardData, CF_BITMAP, CF_DIB,
    CF_HDROP, CF_TEXT, CF_UNICODETEXT,
};

pub struct WindowsClipboard {
    monitoring: bool,
    callback: Option<Box<dyn Fn(ClipboardContent) + Send + 'static>>,
}

impl WindowsClipboard {
    pub fn new() -> Result<Self, ClipboardError> {
        Ok(WindowsClipboard {
            monitoring: false,
            callback: None,
        })
    }

    fn with_clipboard<F, R>(&self, f: F) -> Result<R, ClipboardError>
    where
        F: FnOnce() -> Result<R, ClipboardError>,
    {
        unsafe {
            if OpenClipboard(ptr::null_mut()) == FALSE {
                return Err(ClipboardError::AccessDenied);
            }

            let result = f();
            CloseClipboard();
            result
        }
    }
}

impl Clipboard for WindowsClipboard {
    fn get_content(&self) -> Result<Option<ClipboardContent>, ClipboardError> {
        self.with_clipboard(|| {
            unsafe {
                // Check for text
                if IsClipboardFormatAvailable(CF_UNICODETEXT) != FALSE {
                    let handle = GetClipboardData(CF_UNICODETEXT);
                    if handle.is_null() {
                        return Ok(None);
                    }

                    let text_ptr = GlobalLock(handle) as *const u16;
                    if text_ptr.is_null() {
                        return Ok(None);
                    }

                    // Find string length
                    let mut len = 0;
                    while *text_ptr.offset(len) != 0 {
                        len += 1;
                    }

                    let text_slice = std::slice::from_raw_parts(text_ptr, len as usize);
                    let text = String::from_utf16_lossy(text_slice);

                    GlobalUnlock(handle);

                    return Ok(Some(ClipboardContent {
                        content_type: ClipboardContentType::Text,
                        text: Some(text.clone()),
                        html: None,
                        image_data: None,
                        image_format: None,
                        files: None,
                        size: text.len(),
                        timestamp: SystemTime::now(),
                    }));
                }

                // Check for bitmap
                if IsClipboardFormatAvailable(CF_DIB) != FALSE {
                    let handle = GetClipboardData(CF_DIB);
                    if handle.is_null() {
                        return Ok(None);
                    }

                    let data_ptr = GlobalLock(handle) as *const u8;
                    if data_ptr.is_null() {
                        return Ok(None);
                    }

                    let size = GlobalSize(handle) as usize;
                    let data = std::slice::from_raw_parts(data_ptr, size).to_vec();

                    GlobalUnlock(handle);

                    return Ok(Some(ClipboardContent {
                        content_type: ClipboardContentType::Image,
                        text: None,
                        html: None,
                        image_data: Some(data),
                        image_format: Some("BMP".to_string()),
                        files: None,
                        size,
                        timestamp: SystemTime::now(),
                    }));
                }

                // Check for file drop
                if IsClipboardFormatAvailable(CF_HDROP) != FALSE {
                    // File drop handling would go here
                    // For now, return None
                }

                Ok(None)
            }
        })
    }

    fn set_content(&self, content: ClipboardContent) -> Result<(), ClipboardError> {
        self.with_clipboard(|| {
            unsafe {
                EmptyClipboard();

                match content.content_type {
                    ClipboardContentType::Text => {
                        if let Some(text) = content.text {
                            let wide: Vec<u16> = text.encode_utf16().chain(std::iter::once(0)).collect();
                            let size = wide.len() * 2;
                            
                            let handle = GlobalAlloc(GMEM_MOVEABLE, size);
                            if handle.is_null() {
                                return Err(ClipboardError::PlatformError("Failed to allocate memory".to_string()));
                            }

                            let ptr = GlobalLock(handle) as *mut u16;
                            if ptr.is_null() {
                                GlobalFree(handle);
                                return Err(ClipboardError::PlatformError("Failed to lock memory".to_string()));
                            }

                            ptr::copy_nonoverlapping(wide.as_ptr(), ptr, wide.len());
                            GlobalUnlock(handle);

                            if SetClipboardData(CF_UNICODETEXT, handle) == ptr::null_mut() {
                                GlobalFree(handle);
                                return Err(ClipboardError::PlatformError("Failed to set clipboard data".to_string()));
                            }
                        }
                    }
                    ClipboardContentType::Image => {
                        if let Some(data) = content.image_data {
                            let handle = GlobalAlloc(GMEM_MOVEABLE, data.len());
                            if handle.is_null() {
                                return Err(ClipboardError::PlatformError("Failed to allocate memory".to_string()));
                            }

                            let ptr = GlobalLock(handle) as *mut u8;
                            if ptr.is_null() {
                                GlobalFree(handle);
                                return Err(ClipboardError::PlatformError("Failed to lock memory".to_string()));
                            }

                            ptr::copy_nonoverlapping(data.as_ptr(), ptr, data.len());
                            GlobalUnlock(handle);

                            if SetClipboardData(CF_DIB, handle) == ptr::null_mut() {
                                GlobalFree(handle);
                                return Err(ClipboardError::PlatformError("Failed to set clipboard data".to_string()));
                            }
                        }
                    }
                    _ => return Err(ClipboardError::InvalidFormat),
                }

                Ok(())
            }
        })
    }

    fn clear(&self) -> Result<(), ClipboardError> {
        self.with_clipboard(|| {
            unsafe {
                EmptyClipboard();
                Ok(())
            }
        })
    }

    fn is_format_supported(&self, format: &ClipboardFormat) -> bool {
        unsafe {
            match format {
                ClipboardFormat::Text => IsClipboardFormatAvailable(CF_UNICODETEXT) != FALSE,
                ClipboardFormat::Image => IsClipboardFormatAvailable(CF_DIB) != FALSE || IsClipboardFormatAvailable(CF_BITMAP) != FALSE,
                ClipboardFormat::FileList => IsClipboardFormatAvailable(CF_HDROP) != FALSE,
                ClipboardFormat::Html => {
                    let format_name: Vec<u16> = "HTML Format\0".encode_utf16().collect();
                    let html_format = RegisterClipboardFormatW(format_name.as_ptr());
                    IsClipboardFormatAvailable(html_format) != FALSE
                }
                ClipboardFormat::RichText => {
                    let format_name: Vec<u16> = "Rich Text Format\0".encode_utf16().collect();
                    let rtf_format = RegisterClipboardFormatW(format_name.as_ptr());
                    IsClipboardFormatAvailable(rtf_format) != FALSE
                }
            }
        }
    }

    fn start_monitoring(&mut self, callback: Box<dyn Fn(ClipboardContent) + Send + 'static>) -> Result<(), ClipboardError> {
        self.callback = Some(callback);
        self.monitoring = true;
        
        // In a real implementation, we would:
        // 1. Create a hidden window
        // 2. Add it to the clipboard viewer chain
        // 3. Process WM_DRAWCLIPBOARD messages
        // For now, this is a simplified version
        
        Ok(())
    }

    fn stop_monitoring(&mut self) -> Result<(), ClipboardError> {
        self.monitoring = false;
        self.callback = None;
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_windows_clipboard_creation() {
        let clipboard = WindowsClipboard::new();
        assert!(clipboard.is_ok());
    }

    #[test]
    fn test_format_support() {
        let clipboard = WindowsClipboard::new().unwrap();
        // These tests would actually check the system clipboard
        // In unit tests, we might want to mock this
    }
}