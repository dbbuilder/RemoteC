//! Monitor enumeration and management
//! 
//! Provides functionality for discovering and managing multiple monitors.

use crate::{Result, RemoteCError};
use std::fmt;

/// Represents a physical display monitor
#[derive(Debug, Clone)]
pub struct Monitor {
    /// Unique identifier for the monitor
    pub id: String,
    /// Display index (0-based)
    pub index: usize,
    /// Human-readable name of the monitor
    pub name: String,
    /// Whether this is the primary monitor
    pub is_primary: bool,
    /// Monitor position and dimensions
    pub bounds: MonitorBounds,
    /// Work area (excluding taskbar/dock)
    pub work_area: MonitorBounds,
    /// Display scale factor (DPI scaling)
    pub scale_factor: f32,
    /// Refresh rate in Hz
    pub refresh_rate: u32,
    /// Bit depth
    pub bit_depth: u32,
    /// Monitor orientation
    pub orientation: MonitorOrientation,
}

/// Monitor position and dimensions
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct MonitorBounds {
    /// X coordinate of the top-left corner
    pub x: i32,
    /// Y coordinate of the top-left corner
    pub y: i32,
    /// Width in pixels
    pub width: u32,
    /// Height in pixels
    pub height: u32,
}

/// Monitor orientation
#[derive(Debug, Clone, Copy, PartialEq)]
pub enum MonitorOrientation {
    Landscape,
    Portrait,
    LandscapeFlipped,
    PortraitFlipped,
}

/// Virtual desktop information combining all monitors
#[derive(Debug, Clone)]
pub struct VirtualDesktop {
    /// List of all available monitors
    pub monitors: Vec<Monitor>,
    /// Combined bounds of all monitors
    pub total_bounds: MonitorBounds,
    /// Primary monitor index
    pub primary_index: usize,
}

impl Monitor {
    /// Get the center point of the monitor
    pub fn center(&self) -> (i32, i32) {
        (
            self.bounds.x + (self.bounds.width as i32) / 2,
            self.bounds.y + (self.bounds.height as i32) / 2,
        )
    }

    /// Check if a point is within this monitor
    pub fn contains_point(&self, x: i32, y: i32) -> bool {
        x >= self.bounds.x
            && x < self.bounds.x + self.bounds.width as i32
            && y >= self.bounds.y
            && y < self.bounds.y + self.bounds.height as i32
    }

    /// Get the actual pixel dimensions accounting for scale factor
    pub fn physical_size(&self) -> (u32, u32) {
        (
            (self.bounds.width as f32 * self.scale_factor) as u32,
            (self.bounds.height as f32 * self.scale_factor) as u32,
        )
    }
}

impl MonitorBounds {
    /// Create a new MonitorBounds
    pub fn new(x: i32, y: i32, width: u32, height: u32) -> Self {
        Self { x, y, width, height }
    }

    /// Check if this bounds intersects with another
    pub fn intersects(&self, other: &MonitorBounds) -> bool {
        self.x < other.x + other.width as i32
            && self.x + self.width as i32 > other.x
            && self.y < other.y + other.height as i32
            && self.y + self.height as i32 > other.y
    }

    /// Get the intersection of two bounds
    pub fn intersection(&self, other: &MonitorBounds) -> Option<MonitorBounds> {
        let x1 = self.x.max(other.x);
        let y1 = self.y.max(other.y);
        let x2 = (self.x + self.width as i32).min(other.x + other.width as i32);
        let y2 = (self.y + self.height as i32).min(other.y + other.height as i32);

        if x2 > x1 && y2 > y1 {
            Some(MonitorBounds::new(x1, y1, (x2 - x1) as u32, (y2 - y1) as u32))
        } else {
            None
        }
    }

    /// Get the union of two bounds
    pub fn union(&self, other: &MonitorBounds) -> MonitorBounds {
        let x1 = self.x.min(other.x);
        let y1 = self.y.min(other.y);
        let x2 = (self.x + self.width as i32).max(other.x + other.width as i32);
        let y2 = (self.y + self.height as i32).max(other.y + other.height as i32);

        MonitorBounds::new(x1, y1, (x2 - x1) as u32, (y2 - y1) as u32)
    }
}

impl VirtualDesktop {
    /// Create a new VirtualDesktop from a list of monitors
    pub fn new(monitors: Vec<Monitor>) -> Result<Self> {
        if monitors.is_empty() {
            return Err(RemoteCError::CaptureError("No monitors found".to_string()));
        }

        // Find primary monitor
        let primary_index = monitors
            .iter()
            .position(|m| m.is_primary)
            .unwrap_or(0);

        // Calculate total bounds
        let mut total_bounds = monitors[0].bounds;
        for monitor in &monitors[1..] {
            total_bounds = total_bounds.union(&monitor.bounds);
        }

        Ok(Self {
            monitors,
            total_bounds,
            primary_index,
        })
    }

    /// Get monitor at a specific point
    pub fn monitor_at_point(&self, x: i32, y: i32) -> Option<&Monitor> {
        self.monitors.iter().find(|m| m.contains_point(x, y))
    }

    /// Get monitor by index
    pub fn get_monitor(&self, index: usize) -> Option<&Monitor> {
        self.monitors.get(index)
    }

    /// Get the primary monitor
    pub fn primary_monitor(&self) -> &Monitor {
        &self.monitors[self.primary_index]
    }

    /// Get all monitors sorted by position (left to right, top to bottom)
    pub fn monitors_sorted(&self) -> Vec<&Monitor> {
        let mut sorted: Vec<&Monitor> = self.monitors.iter().collect();
        sorted.sort_by(|a, b| {
            match a.bounds.y.cmp(&b.bounds.y) {
                std::cmp::Ordering::Equal => a.bounds.x.cmp(&b.bounds.x),
                other => other,
            }
        });
        sorted
    }
}

impl fmt::Display for Monitor {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "{} ({}) - {}x{} @ {}Hz{}",
            self.name,
            self.id,
            self.bounds.width,
            self.bounds.height,
            self.refresh_rate,
            if self.is_primary { " [Primary]" } else { "" }
        )
    }
}

impl fmt::Display for MonitorOrientation {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            MonitorOrientation::Landscape => write!(f, "Landscape"),
            MonitorOrientation::Portrait => write!(f, "Portrait"),
            MonitorOrientation::LandscapeFlipped => write!(f, "Landscape (Flipped)"),
            MonitorOrientation::PortraitFlipped => write!(f, "Portrait (Flipped)"),
        }
    }
}

/// Platform-specific monitor enumeration
#[cfg(target_os = "windows")]
pub fn enumerate_monitors() -> Result<Vec<Monitor>> {
    super::windows::enumerate_monitors_windows()
}

#[cfg(target_os = "linux")]
pub fn enumerate_monitors() -> Result<Vec<Monitor>> {
    super::linux::enumerate_monitors_linux()
}

#[cfg(target_os = "macos")]
pub fn enumerate_monitors() -> Result<Vec<Monitor>> {
    super::macos::enumerate_monitors_macos()
}

#[cfg(not(any(target_os = "windows", target_os = "linux", target_os = "macos")))]
pub fn enumerate_monitors() -> Result<Vec<Monitor>> {
    Err(RemoteCError::UnsupportedPlatform(
        "Monitor enumeration not supported on this platform".to_string()
    ))
}

/// Get the virtual desktop information
pub fn get_virtual_desktop() -> Result<VirtualDesktop> {
    let monitors = enumerate_monitors()?;
    VirtualDesktop::new(monitors)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_monitor_bounds_intersection() {
        let bounds1 = MonitorBounds::new(0, 0, 100, 100);
        let bounds2 = MonitorBounds::new(50, 50, 100, 100);
        let bounds3 = MonitorBounds::new(200, 200, 100, 100);

        assert!(bounds1.intersects(&bounds2));
        assert!(!bounds1.intersects(&bounds3));

        let intersection = bounds1.intersection(&bounds2).unwrap();
        assert_eq!(intersection.x, 50);
        assert_eq!(intersection.y, 50);
        assert_eq!(intersection.width, 50);
        assert_eq!(intersection.height, 50);
    }

    #[test]
    fn test_monitor_bounds_union() {
        let bounds1 = MonitorBounds::new(0, 0, 100, 100);
        let bounds2 = MonitorBounds::new(50, 50, 100, 100);

        let union = bounds1.union(&bounds2);
        assert_eq!(union.x, 0);
        assert_eq!(union.y, 0);
        assert_eq!(union.width, 150);
        assert_eq!(union.height, 150);
    }

    #[test]
    fn test_monitor_contains_point() {
        let monitor = Monitor {
            id: "test".to_string(),
            index: 0,
            name: "Test Monitor".to_string(),
            is_primary: true,
            bounds: MonitorBounds::new(0, 0, 1920, 1080),
            work_area: MonitorBounds::new(0, 0, 1920, 1040),
            scale_factor: 1.0,
            refresh_rate: 60,
            bit_depth: 32,
            orientation: MonitorOrientation::Landscape,
        };

        assert!(monitor.contains_point(960, 540));
        assert!(monitor.contains_point(0, 0));
        assert!(monitor.contains_point(1919, 1079));
        assert!(!monitor.contains_point(1920, 1080));
        assert!(!monitor.contains_point(-1, 0));
    }

    #[test]
    fn test_virtual_desktop() {
        let monitors = vec![
            Monitor {
                id: "primary".to_string(),
                index: 0,
                name: "Primary Monitor".to_string(),
                is_primary: true,
                bounds: MonitorBounds::new(0, 0, 1920, 1080),
                work_area: MonitorBounds::new(0, 0, 1920, 1040),
                scale_factor: 1.0,
                refresh_rate: 60,
                bit_depth: 32,
                orientation: MonitorOrientation::Landscape,
            },
            Monitor {
                id: "secondary".to_string(),
                index: 1,
                name: "Secondary Monitor".to_string(),
                is_primary: false,
                bounds: MonitorBounds::new(1920, 0, 1920, 1080),
                work_area: MonitorBounds::new(1920, 0, 1920, 1040),
                scale_factor: 1.0,
                refresh_rate: 60,
                bit_depth: 32,
                orientation: MonitorOrientation::Landscape,
            },
        ];

        let desktop = VirtualDesktop::new(monitors).unwrap();
        assert_eq!(desktop.total_bounds.width, 3840);
        assert_eq!(desktop.total_bounds.height, 1080);
        assert_eq!(desktop.primary_index, 0);

        assert!(desktop.monitor_at_point(100, 100).unwrap().is_primary);
        assert!(!desktop.monitor_at_point(2000, 100).unwrap().is_primary);
    }
}