//! Input simulation module for RemoteC Core
//! 
//! Provides cross-platform input simulation for mouse and keyboard events.

use crate::{Result, RemoteCError};

/// Mouse button types
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum MouseButton {
    /// Left mouse button
    Left,
    /// Right mouse button
    Right,
    /// Middle mouse button
    Middle,
    /// Additional button 1
    Button4,
    /// Additional button 2
    Button5,
}

/// Mouse event types
#[derive(Debug, Clone)]
pub enum MouseEvent {
    /// Move mouse to absolute position
    Move { x: i32, y: i32 },
    /// Move mouse relative to current position
    MoveRelative { dx: i32, dy: i32 },
    /// Button down event
    ButtonDown { button: MouseButton },
    /// Button up event
    ButtonUp { button: MouseButton },
    /// Button click event (down + up)
    Click { button: MouseButton },
    /// Double click event
    DoubleClick { button: MouseButton },
    /// Scroll wheel event
    Scroll { delta: i32, horizontal: bool },
}

/// Keyboard key codes
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum KeyCode {
    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    
    // Numbers
    Num0, Num1, Num2, Num3, Num4,
    Num5, Num6, Num7, Num8, Num9,
    
    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8,
    F9, F10, F11, F12,
    
    // Modifiers
    Shift, Control, Alt, Meta,
    
    // Navigation
    Left, Right, Up, Down,
    Home, End, PageUp, PageDown,
    
    // Special keys
    Enter, Space, Tab, Backspace, Delete,
    Escape, CapsLock, NumLock, ScrollLock,
    
    // Punctuation
    Period, Comma, Semicolon, Quote,
    Slash, Backslash, Minus, Equals,
    LeftBracket, RightBracket,
}

/// Keyboard event types
#[derive(Debug, Clone)]
pub enum KeyboardEvent {
    /// Key down event
    KeyDown { code: KeyCode },
    /// Key up event
    KeyUp { code: KeyCode },
    /// Key press event (down + up)
    KeyPress { code: KeyCode },
    /// Type text
    TypeText { text: String },
}

/// Trait for input simulation implementations
pub trait InputSimulator: Send + Sync {
    /// Simulate a mouse event
    fn mouse_event(&mut self, event: MouseEvent) -> Result<()>;
    
    /// Simulate a keyboard event
    fn keyboard_event(&mut self, event: KeyboardEvent) -> Result<()>;
    
    /// Get current mouse position
    fn get_mouse_position(&self) -> Result<(i32, i32)>;
    
    /// Check if a key is currently pressed
    fn is_key_pressed(&self, code: KeyCode) -> Result<bool>;
}

#[cfg(test)]
mod tests;

// Platform-specific implementations
#[cfg(target_os = "windows")]
pub mod windows;

#[cfg(target_os = "linux")]
pub mod linux;

#[cfg(target_os = "macos")]
pub mod macos;

/// Create a platform-specific input simulator instance
pub fn create_simulator() -> Result<Box<dyn InputSimulator>> {
    #[cfg(target_os = "windows")]
    {
        Ok(Box::new(windows::WindowsInputSimulator::new()?))
    }
    
    #[cfg(target_os = "linux")]
    {
        Ok(Box::new(linux::LinuxInputSimulator::new()?))
    }
    
    #[cfg(target_os = "macos")]
    {
        Ok(Box::new(macos::MacOSInputSimulator::new()?))
    }
    
    #[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
    {
        Err(RemoteCError::UnsupportedPlatform(
            "Input simulation not supported on this platform".to_string()
        ))
    }
}