use super::{Clipboard, ClipboardContent, ClipboardError, ClipboardFormat};

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
}

impl Clipboard for WindowsClipboard {
    fn get_content(&self) -> Result<Option<ClipboardContent>, ClipboardError> {
        // Stub implementation - return empty clipboard
        Ok(None)
    }

    fn set_content(&self, _content: ClipboardContent) -> Result<(), ClipboardError> {
        // Stub implementation - do nothing
        log::warn!("Windows clipboard implementation is stubbed");
        Ok(())
    }

    fn clear(&self) -> Result<(), ClipboardError> {
        // Stub implementation - do nothing
        log::warn!("Windows clipboard implementation is stubbed");
        Ok(())
    }

    fn is_format_supported(&self, _format: &ClipboardFormat) -> bool {
        // Stub implementation - return false for all formats
        false
    }

    fn start_monitoring(&mut self, callback: Box<dyn Fn(ClipboardContent) + Send + 'static>) -> Result<(), ClipboardError> {
        // Stub implementation - store callback but don't monitor
        self.callback = Some(callback);
        self.monitoring = true;
        log::warn!("Windows clipboard monitoring is stubbed");
        Ok(())
    }

    fn stop_monitoring(&mut self) -> Result<(), ClipboardError> {
        // Stub implementation - just clear monitoring state
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
    fn test_windows_clipboard_operations() {
        let clipboard = WindowsClipboard::new().unwrap();
        
        // Test get_content
        let content = clipboard.get_content();
        assert!(content.is_ok());
        assert!(content.unwrap().is_none());

        // Test set_content
        let test_content = ClipboardContent {
            content_type: ClipboardContentType::Text,
            text: Some("Test".to_string()),
            html: None,
            image_data: None,
            image_format: None,
            files: None,
            size: 4,
            timestamp: SystemTime::now(),
        };
        let result = clipboard.set_content(test_content);
        assert!(result.is_ok());

        // Test clear
        let clear_result = clipboard.clear();
        assert!(clear_result.is_ok());

        // Test format support
        assert!(!clipboard.is_format_supported(&ClipboardFormat::Text));
    }

    #[test]
    fn test_windows_clipboard_monitoring() {
        let mut clipboard = WindowsClipboard::new().unwrap();
        
        // Test start monitoring
        let callback = Box::new(|_content: ClipboardContent| {
            // Test callback
        });
        let start_result = clipboard.start_monitoring(callback);
        assert!(start_result.is_ok());
        assert!(clipboard.monitoring);

        // Test stop monitoring
        let stop_result = clipboard.stop_monitoring();
        assert!(stop_result.is_ok());
        assert!(!clipboard.monitoring);
    }
}