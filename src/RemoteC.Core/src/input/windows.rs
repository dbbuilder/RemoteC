//! Windows-specific input simulation implementation

use super::{InputSimulator, KeyCode, KeyboardEvent, MouseButton, MouseEvent};
use crate::{Result, RemoteCError};
use winapi::{
    shared::{
        minwindef::{DWORD, WORD},
        windef::POINT,
    },
    um::{
        winuser::{
            GetAsyncKeyState, GetCursorPos, GetSystemMetrics, SendInput, INPUT, INPUT_KEYBOARD,
            INPUT_MOUSE, KEYBDINPUT, KEYEVENTF_KEYUP, KEYEVENTF_UNICODE, MOUSEEVENTF_ABSOLUTE,
            MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP, MOUSEEVENTF_MIDDLEDOWN,
            MOUSEEVENTF_MIDDLEUP, MOUSEEVENTF_MOVE, MOUSEEVENTF_RIGHTDOWN,
            MOUSEEVENTF_RIGHTUP, MOUSEEVENTF_WHEEL, MOUSEEVENTF_HWHEEL, MOUSEEVENTF_XDOWN,
            MOUSEEVENTF_XUP, MOUSEINPUT, SM_CXSCREEN, SM_CYSCREEN, VK_CONTROL, VK_SHIFT,
            VK_MENU, VK_LWIN, WHEEL_DELTA, XBUTTON1, XBUTTON2,
        },
    },
};
use std::mem::{size_of, zeroed};

pub struct WindowsInputSimulator {
    screen_width: i32,
    screen_height: i32,
}

impl WindowsInputSimulator {
    pub fn new() -> Result<Self> {
        unsafe {
            let screen_width = GetSystemMetrics(SM_CXSCREEN);
            let screen_height = GetSystemMetrics(SM_CYSCREEN);
            
            if screen_width <= 0 || screen_height <= 0 {
                return Err(RemoteCError::InitializationFailed(
                    "Failed to get screen dimensions".to_string()
                ));
            }
            
            Ok(Self {
                screen_width,
                screen_height,
            })
        }
    }
    
    fn keycode_to_vk(code: KeyCode) -> u16 {
        match code {
            // Letters
            KeyCode::A => b'A' as u16,
            KeyCode::B => b'B' as u16,
            KeyCode::C => b'C' as u16,
            KeyCode::D => b'D' as u16,
            KeyCode::E => b'E' as u16,
            KeyCode::F => b'F' as u16,
            KeyCode::G => b'G' as u16,
            KeyCode::H => b'H' as u16,
            KeyCode::I => b'I' as u16,
            KeyCode::J => b'J' as u16,
            KeyCode::K => b'K' as u16,
            KeyCode::L => b'L' as u16,
            KeyCode::M => b'M' as u16,
            KeyCode::N => b'N' as u16,
            KeyCode::O => b'O' as u16,
            KeyCode::P => b'P' as u16,
            KeyCode::Q => b'Q' as u16,
            KeyCode::R => b'R' as u16,
            KeyCode::S => b'S' as u16,
            KeyCode::T => b'T' as u16,
            KeyCode::U => b'U' as u16,
            KeyCode::V => b'V' as u16,
            KeyCode::W => b'W' as u16,
            KeyCode::X => b'X' as u16,
            KeyCode::Y => b'Y' as u16,
            KeyCode::Z => b'Z' as u16,
            
            // Numbers
            KeyCode::Num0 => b'0' as u16,
            KeyCode::Num1 => b'1' as u16,
            KeyCode::Num2 => b'2' as u16,
            KeyCode::Num3 => b'3' as u16,
            KeyCode::Num4 => b'4' as u16,
            KeyCode::Num5 => b'5' as u16,
            KeyCode::Num6 => b'6' as u16,
            KeyCode::Num7 => b'7' as u16,
            KeyCode::Num8 => b'8' as u16,
            KeyCode::Num9 => b'9' as u16,
            
            // Function keys
            KeyCode::F1 => 0x70,
            KeyCode::F2 => 0x71,
            KeyCode::F3 => 0x72,
            KeyCode::F4 => 0x73,
            KeyCode::F5 => 0x74,
            KeyCode::F6 => 0x75,
            KeyCode::F7 => 0x76,
            KeyCode::F8 => 0x77,
            KeyCode::F9 => 0x78,
            KeyCode::F10 => 0x79,
            KeyCode::F11 => 0x7A,
            KeyCode::F12 => 0x7B,
            
            // Modifiers
            KeyCode::Shift => VK_SHIFT as u16,
            KeyCode::Control => VK_CONTROL as u16,
            KeyCode::Alt => VK_MENU as u16,
            KeyCode::Meta => VK_LWIN as u16,
            
            // Navigation
            KeyCode::Left => 0x25,
            KeyCode::Up => 0x26,
            KeyCode::Right => 0x27,
            KeyCode::Down => 0x28,
            KeyCode::Home => 0x24,
            KeyCode::End => 0x23,
            KeyCode::PageUp => 0x21,
            KeyCode::PageDown => 0x22,
            
            // Special keys
            KeyCode::Enter => 0x0D,
            KeyCode::Space => 0x20,
            KeyCode::Tab => 0x09,
            KeyCode::Backspace => 0x08,
            KeyCode::Delete => 0x2E,
            KeyCode::Escape => 0x1B,
            KeyCode::CapsLock => 0x14,
            KeyCode::NumLock => 0x90,
            KeyCode::ScrollLock => 0x91,
            
            // Punctuation
            KeyCode::Period => 0xBE,
            KeyCode::Comma => 0xBC,
            KeyCode::Semicolon => 0xBA,
            KeyCode::Quote => 0xDE,
            KeyCode::Slash => 0xBF,
            KeyCode::Backslash => 0xDC,
            KeyCode::Minus => 0xBD,
            KeyCode::Equals => 0xBB,
            KeyCode::LeftBracket => 0xDB,
            KeyCode::RightBracket => 0xDD,
        }
    }
    
    fn mouse_button_flags(button: MouseButton, down: bool) -> DWORD {
        match (button, down) {
            (MouseButton::Left, true) => MOUSEEVENTF_LEFTDOWN,
            (MouseButton::Left, false) => MOUSEEVENTF_LEFTUP,
            (MouseButton::Right, true) => MOUSEEVENTF_RIGHTDOWN,
            (MouseButton::Right, false) => MOUSEEVENTF_RIGHTUP,
            (MouseButton::Middle, true) => MOUSEEVENTF_MIDDLEDOWN,
            (MouseButton::Middle, false) => MOUSEEVENTF_MIDDLEUP,
            (MouseButton::Button4, true) => MOUSEEVENTF_XDOWN,
            (MouseButton::Button4, false) => MOUSEEVENTF_XUP,
            (MouseButton::Button5, true) => MOUSEEVENTF_XDOWN,
            (MouseButton::Button5, false) => MOUSEEVENTF_XUP,
        }
    }
    
    fn send_mouse_input(&self, flags: DWORD, dx: i32, dy: i32, data: DWORD) -> Result<()> {
        unsafe {
            let mut input: INPUT = zeroed();
            input.type_ = INPUT_MOUSE;
            
            let mouse = input.u.mi_mut();
            mouse.dx = dx;
            mouse.dy = dy;
            mouse.mouseData = data;
            mouse.dwFlags = flags;
            mouse.time = 0;
            mouse.dwExtraInfo = 0;
            
            let result = SendInput(1, &mut input, size_of::<INPUT>() as i32);
            if result == 0 {
                return Err(RemoteCError::InputError(
                    "Failed to send mouse input".to_string()
                ));
            }
            
            Ok(())
        }
    }
    
    fn send_keyboard_input(&self, vk: u16, scan: u16, flags: DWORD) -> Result<()> {
        unsafe {
            let mut input: INPUT = zeroed();
            input.type_ = INPUT_KEYBOARD;
            
            let keyboard = input.u.ki_mut();
            keyboard.wVk = vk;
            keyboard.wScan = scan;
            keyboard.dwFlags = flags;
            keyboard.time = 0;
            keyboard.dwExtraInfo = 0;
            
            let result = SendInput(1, &mut input, size_of::<INPUT>() as i32);
            if result == 0 {
                return Err(RemoteCError::InputError(
                    "Failed to send keyboard input".to_string()
                ));
            }
            
            Ok(())
        }
    }
}

impl InputSimulator for WindowsInputSimulator {
    fn mouse_event(&mut self, event: MouseEvent) -> Result<()> {
        match event {
            MouseEvent::Move { x, y } => {
                // Convert to normalized absolute coordinates (0-65535)
                let norm_x = (x * 65535) / self.screen_width;
                let norm_y = (y * 65535) / self.screen_height;
                self.send_mouse_input(
                    MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                    norm_x,
                    norm_y,
                    0
                )
            }
            MouseEvent::MoveRelative { dx, dy } => {
                self.send_mouse_input(MOUSEEVENTF_MOVE, dx, dy, 0)
            }
            MouseEvent::ButtonDown { button } => {
                let flags = Self::mouse_button_flags(button, true);
                let data = match button {
                    MouseButton::Button4 => XBUTTON1,
                    MouseButton::Button5 => XBUTTON2,
                    _ => 0,
                };
                self.send_mouse_input(flags, 0, 0, data.into())
            }
            MouseEvent::ButtonUp { button } => {
                let flags = Self::mouse_button_flags(button, false);
                let data = match button {
                    MouseButton::Button4 => XBUTTON1,
                    MouseButton::Button5 => XBUTTON2,
                    _ => 0,
                };
                self.send_mouse_input(flags, 0, 0, data.into())
            }
            MouseEvent::Click { button } => {
                self.mouse_event(MouseEvent::ButtonDown { button })?;
                self.mouse_event(MouseEvent::ButtonUp { button })
            }
            MouseEvent::DoubleClick { button } => {
                self.mouse_event(MouseEvent::Click { button })?;
                std::thread::sleep(std::time::Duration::from_millis(50));
                self.mouse_event(MouseEvent::Click { button })
            }
            MouseEvent::Scroll { delta, horizontal } => {
                let flags = if horizontal {
                    MOUSEEVENTF_HWHEEL
                } else {
                    MOUSEEVENTF_WHEEL
                };
                // Windows uses WHEEL_DELTA (120) as one notch
                let wheel_delta = (delta * WHEEL_DELTA as i32) as DWORD;
                self.send_mouse_input(flags, 0, 0, wheel_delta)
            }
        }
    }
    
    fn keyboard_event(&mut self, event: KeyboardEvent) -> Result<()> {
        match event {
            KeyboardEvent::KeyDown { code } => {
                let vk = Self::keycode_to_vk(code);
                self.send_keyboard_input(vk, 0, 0)
            }
            KeyboardEvent::KeyUp { code } => {
                let vk = Self::keycode_to_vk(code);
                self.send_keyboard_input(vk, 0, KEYEVENTF_KEYUP)
            }
            KeyboardEvent::KeyPress { code } => {
                self.keyboard_event(KeyboardEvent::KeyDown { code })?;
                self.keyboard_event(KeyboardEvent::KeyUp { code })
            }
            KeyboardEvent::TypeText { text } => {
                // Send each character as Unicode
                for ch in text.chars() {
                    let scan = ch as u16;
                    self.send_keyboard_input(0, scan, KEYEVENTF_UNICODE)?;
                    self.send_keyboard_input(0, scan, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP)?;
                }
                Ok(())
            }
        }
    }
    
    fn get_mouse_position(&self) -> Result<(i32, i32)> {
        unsafe {
            let mut point: POINT = zeroed();
            if GetCursorPos(&mut point) == 0 {
                return Err(RemoteCError::InputError(
                    "Failed to get cursor position".to_string()
                ));
            }
            Ok((point.x, point.y))
        }
    }
    
    fn is_key_pressed(&self, code: KeyCode) -> Result<bool> {
        unsafe {
            let vk = Self::keycode_to_vk(code) as i32;
            let state = GetAsyncKeyState(vk);
            // High bit set means key is pressed
            // 0x8000 as i16 is -32768 in two's complement
            Ok((state & (0x8000u16 as i16)) != 0)
        }
    }
}