use super::*;
use std::collections::HashMap;

#[test]
fn test_mouse_button() {
    assert_eq!(MouseButton::Left, MouseButton::Left);
    assert_ne!(MouseButton::Left, MouseButton::Right);
}

#[test]
fn test_key_code() {
    assert_eq!(KeyCode::A, KeyCode::A);
    assert_ne!(KeyCode::A, KeyCode::B);
}

// Mock implementation for testing
struct MockInputSimulator {
    mouse_position: (i32, i32),
    pressed_keys: HashMap<KeyCode, bool>,
    events_log: Vec<String>,
}

impl MockInputSimulator {
    fn new() -> Self {
        Self {
            mouse_position: (0, 0),
            pressed_keys: HashMap::new(),
            events_log: Vec::new(),
        }
    }
}

impl InputSimulator for MockInputSimulator {
    fn mouse_event(&mut self, event: MouseEvent) -> Result<()> {
        match event {
            MouseEvent::Move { x, y } => {
                self.mouse_position = (x, y);
                self.events_log.push(format!("Mouse moved to ({}, {})", x, y));
            }
            MouseEvent::MoveRelative { dx, dy } => {
                self.mouse_position.0 += dx;
                self.mouse_position.1 += dy;
                self.events_log.push(format!("Mouse moved relative ({}, {})", dx, dy));
            }
            MouseEvent::ButtonDown { button } => {
                self.events_log.push(format!("Mouse button {:?} down", button));
            }
            MouseEvent::ButtonUp { button } => {
                self.events_log.push(format!("Mouse button {:?} up", button));
            }
            MouseEvent::Click { button } => {
                self.events_log.push(format!("Mouse button {:?} clicked", button));
            }
            MouseEvent::DoubleClick { button } => {
                self.events_log.push(format!("Mouse button {:?} double-clicked", button));
            }
            MouseEvent::Scroll { delta, horizontal } => {
                let direction = if horizontal { "horizontal" } else { "vertical" };
                self.events_log.push(format!("Mouse scroll {} delta {}", direction, delta));
            }
        }
        Ok(())
    }
    
    fn keyboard_event(&mut self, event: KeyboardEvent) -> Result<()> {
        match event {
            KeyboardEvent::KeyDown { code } => {
                self.pressed_keys.insert(code, true);
                self.events_log.push(format!("Key {:?} down", code));
            }
            KeyboardEvent::KeyUp { code } => {
                self.pressed_keys.insert(code, false);
                self.events_log.push(format!("Key {:?} up", code));
            }
            KeyboardEvent::KeyPress { code } => {
                self.events_log.push(format!("Key {:?} pressed", code));
            }
            KeyboardEvent::TypeText { text } => {
                self.events_log.push(format!("Typed text: '{}'", text));
            }
        }
        Ok(())
    }
    
    fn get_mouse_position(&self) -> Result<(i32, i32)> {
        Ok(self.mouse_position)
    }
    
    fn is_key_pressed(&self, code: KeyCode) -> Result<bool> {
        Ok(*self.pressed_keys.get(&code).unwrap_or(&false))
    }
}

#[test]
fn test_mouse_move() {
    let mut sim = MockInputSimulator::new();
    
    // Test absolute move
    sim.mouse_event(MouseEvent::Move { x: 100, y: 200 }).unwrap();
    assert_eq!(sim.get_mouse_position().unwrap(), (100, 200));
    
    // Test relative move
    sim.mouse_event(MouseEvent::MoveRelative { dx: 50, dy: -30 }).unwrap();
    assert_eq!(sim.get_mouse_position().unwrap(), (150, 170));
}

#[test]
fn test_mouse_buttons() {
    let mut sim = MockInputSimulator::new();
    
    // Test button events
    sim.mouse_event(MouseEvent::ButtonDown { button: MouseButton::Left }).unwrap();
    assert!(sim.events_log.contains(&"Mouse button Left down".to_string()));
    
    sim.mouse_event(MouseEvent::ButtonUp { button: MouseButton::Left }).unwrap();
    assert!(sim.events_log.contains(&"Mouse button Left up".to_string()));
    
    sim.mouse_event(MouseEvent::Click { button: MouseButton::Right }).unwrap();
    assert!(sim.events_log.contains(&"Mouse button Right clicked".to_string()));
    
    sim.mouse_event(MouseEvent::DoubleClick { button: MouseButton::Left }).unwrap();
    assert!(sim.events_log.contains(&"Mouse button Left double-clicked".to_string()));
}

#[test]
fn test_mouse_scroll() {
    let mut sim = MockInputSimulator::new();
    
    // Test vertical scroll
    sim.mouse_event(MouseEvent::Scroll { delta: 3, horizontal: false }).unwrap();
    assert!(sim.events_log.contains(&"Mouse scroll vertical delta 3".to_string()));
    
    // Test horizontal scroll
    sim.mouse_event(MouseEvent::Scroll { delta: -2, horizontal: true }).unwrap();
    assert!(sim.events_log.contains(&"Mouse scroll horizontal delta -2".to_string()));
}

#[test]
fn test_keyboard_events() {
    let mut sim = MockInputSimulator::new();
    
    // Test key down
    sim.keyboard_event(KeyboardEvent::KeyDown { code: KeyCode::A }).unwrap();
    assert!(sim.is_key_pressed(KeyCode::A).unwrap());
    assert!(sim.events_log.contains(&"Key A down".to_string()));
    
    // Test key up
    sim.keyboard_event(KeyboardEvent::KeyUp { code: KeyCode::A }).unwrap();
    assert!(!sim.is_key_pressed(KeyCode::A).unwrap());
    assert!(sim.events_log.contains(&"Key A up".to_string()));
    
    // Test key press
    sim.keyboard_event(KeyboardEvent::KeyPress { code: KeyCode::Enter }).unwrap();
    assert!(sim.events_log.contains(&"Key Enter pressed".to_string()));
}

#[test]
fn test_type_text() {
    let mut sim = MockInputSimulator::new();
    
    sim.keyboard_event(KeyboardEvent::TypeText { 
        text: "Hello, World!".to_string() 
    }).unwrap();
    
    assert!(sim.events_log.contains(&"Typed text: 'Hello, World!'".to_string()));
}

#[test]
fn test_modifier_keys() {
    let mut sim = MockInputSimulator::new();
    
    // Test modifier keys
    let modifiers = vec![KeyCode::Shift, KeyCode::Control, KeyCode::Alt, KeyCode::Meta];
    
    for modifier in modifiers {
        sim.keyboard_event(KeyboardEvent::KeyDown { code: modifier }).unwrap();
        assert!(sim.is_key_pressed(modifier).unwrap());
        
        sim.keyboard_event(KeyboardEvent::KeyUp { code: modifier }).unwrap();
        assert!(!sim.is_key_pressed(modifier).unwrap());
    }
}

#[cfg(test)]
mod platform_tests {
    use super::*;
    
    #[test]
    fn test_create_simulator() {
        let result = create_simulator();
        
        // Should succeed on supported platforms
        #[cfg(any(target_os = "windows", target_os = "linux", target_os = "macos"))]
        assert!(result.is_ok());
        
        // Should fail on unsupported platforms
        #[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
        assert!(result.is_err());
    }
}