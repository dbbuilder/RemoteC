use super::*;
use std::time::Duration;

#[test]
fn test_capture_config_default() {
    let config = CaptureConfig::default();
    assert_eq!(config.display_index, 0);
    assert_eq!(config.target_fps, 30);
    assert!(config.capture_cursor);
    assert!(config.region.is_none());
}

#[test]
fn test_capture_region() {
    let region = CaptureRegion {
        x: 100,
        y: 200,
        width: 800,
        height: 600,
    };
    
    assert_eq!(region.x, 100);
    assert_eq!(region.y, 200);
    assert_eq!(region.width, 800);
    assert_eq!(region.height, 600);
}

#[test]
fn test_screen_frame() {
    let frame = ScreenFrame {
        width: 1920,
        height: 1080,
        data: vec![0; 1920 * 1080 * 4], // BGRA format
        timestamp: std::time::Instant::now(),
    };
    
    assert_eq!(frame.width, 1920);
    assert_eq!(frame.height, 1080);
    assert_eq!(frame.data.len(), 1920 * 1080 * 4);
}

// Mock implementation for testing
struct MockCapture {
    config: CaptureConfig,
    active: bool,
    frames: Vec<ScreenFrame>,
    current_frame: usize,
}

impl MockCapture {
    fn new(config: CaptureConfig) -> Self {
        Self {
            config,
            active: false,
            frames: Vec::new(),
            current_frame: 0,
        }
    }
    
    fn add_test_frame(&mut self, width: u32, height: u32) {
        self.frames.push(ScreenFrame {
            width,
            height,
            data: vec![0; (width * height * 4) as usize],
            timestamp: std::time::Instant::now(),
        });
    }
}

impl ScreenCapture for MockCapture {
    fn start(&mut self) -> Result<()> {
        if self.active {
            return Err(RemoteCError::CaptureError("Already capturing".to_string()));
        }
        self.active = true;
        Ok(())
    }
    
    fn stop(&mut self) -> Result<()> {
        if !self.active {
            return Err(RemoteCError::CaptureError("Not capturing".to_string()));
        }
        self.active = false;
        Ok(())
    }
    
    fn get_frame(&mut self) -> Result<Option<ScreenFrame>> {
        if !self.active {
            return Err(RemoteCError::CaptureError("Not capturing".to_string()));
        }
        
        if self.frames.is_empty() {
            return Ok(None);
        }
        
        let frame = self.frames[self.current_frame].clone();
        self.current_frame = (self.current_frame + 1) % self.frames.len();
        Ok(Some(frame))
    }
    
    fn is_active(&self) -> bool {
        self.active
    }
    
    fn config(&self) -> &CaptureConfig {
        &self.config
    }
}

#[test]
fn test_mock_capture_lifecycle() {
    let config = CaptureConfig::default();
    let mut capture = MockCapture::new(config);
    
    // Initially not active
    assert!(!capture.is_active());
    
    // Start capture
    assert!(capture.start().is_ok());
    assert!(capture.is_active());
    
    // Cannot start again
    assert!(capture.start().is_err());
    
    // Stop capture
    assert!(capture.stop().is_ok());
    assert!(!capture.is_active());
    
    // Cannot stop again
    assert!(capture.stop().is_err());
}

#[test]
fn test_mock_capture_frames() {
    let config = CaptureConfig::default();
    let mut capture = MockCapture::new(config);
    
    // Add test frames
    capture.add_test_frame(1920, 1080);
    capture.add_test_frame(1280, 720);
    
    // Cannot get frame when not active
    assert!(capture.get_frame().is_err());
    
    // Start capture
    capture.start().unwrap();
    
    // Get first frame
    let frame1 = capture.get_frame().unwrap().unwrap();
    assert_eq!(frame1.width, 1920);
    assert_eq!(frame1.height, 1080);
    
    // Get second frame
    let frame2 = capture.get_frame().unwrap().unwrap();
    assert_eq!(frame2.width, 1280);
    assert_eq!(frame2.height, 720);
    
    // Frames should cycle
    let frame3 = capture.get_frame().unwrap().unwrap();
    assert_eq!(frame3.width, 1920);
    assert_eq!(frame3.height, 1080);
}

#[test]
fn test_mock_capture_config() {
    let config = CaptureConfig {
        display_index: 1,
        target_fps: 60,
        capture_cursor: false,
        region: Some(CaptureRegion {
            x: 0,
            y: 0,
            width: 1024,
            height: 768,
        }),
    };
    
    let capture = MockCapture::new(config.clone());
    let stored_config = capture.config();
    
    assert_eq!(stored_config.display_index, 1);
    assert_eq!(stored_config.target_fps, 60);
    assert!(!stored_config.capture_cursor);
    assert!(stored_config.region.is_some());
    
    let region = stored_config.region.unwrap();
    assert_eq!(region.width, 1024);
    assert_eq!(region.height, 768);
}

#[cfg(test)]
mod platform_tests {
    use super::*;
    
    #[test]
    fn test_create_capture() {
        let config = CaptureConfig::default();
        let result = create_capture(config);
        
        // Should succeed on supported platforms
        #[cfg(any(target_os = "windows", target_os = "linux", target_os = "macos"))]
        assert!(result.is_ok());
        
        // Should fail on unsupported platforms
        #[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
        assert!(result.is_err());
    }
}