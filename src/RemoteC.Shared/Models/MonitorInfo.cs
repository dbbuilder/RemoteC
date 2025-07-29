using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Information about a physical display monitor
    /// </summary>
    public class MonitorInfo
    {
        /// <summary>
        /// Unique identifier for the monitor
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display index (0-based)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Human-readable name of the monitor
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is the primary monitor
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Monitor position and dimensions
        /// </summary>
        public Rectangle Bounds { get; set; } = new();

        /// <summary>
        /// Work area (excluding taskbar/dock)
        /// </summary>
        public Rectangle WorkArea { get; set; } = new();

        /// <summary>
        /// Display scale factor (DPI scaling)
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        /// <summary>
        /// Refresh rate in Hz
        /// </summary>
        public int RefreshRate { get; set; } = 60;

        /// <summary>
        /// Bit depth
        /// </summary>
        public int BitDepth { get; set; } = 32;

        /// <summary>
        /// Monitor orientation
        /// </summary>
        public MonitorOrientation Orientation { get; set; } = MonitorOrientation.Landscape;

        /// <summary>
        /// Physical width in millimeters
        /// </summary>
        public int PhysicalWidth { get; set; }

        /// <summary>
        /// Physical height in millimeters
        /// </summary>
        public int PhysicalHeight { get; set; }
    }

    /// <summary>
    /// Rectangle structure for bounds
    /// </summary>
    public class Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Right => X + Width;
        public int Bottom => Y + Height;

        public bool Contains(int x, int y)
        {
            return x >= X && x < Right && y >= Y && y < Bottom;
        }

        public bool IntersectsWith(Rectangle other)
        {
            return X < other.Right && Right > other.X && 
                   Y < other.Bottom && Bottom > other.Y;
        }
    }

    /// <summary>
    /// Monitor orientation
    /// </summary>
    public enum MonitorOrientation
    {
        Landscape = 0,
        Portrait = 1,
        LandscapeFlipped = 2,
        PortraitFlipped = 3
    }

    /// <summary>
    /// Virtual desktop information combining all monitors
    /// </summary>
    public class VirtualDesktopInfo
    {
        /// <summary>
        /// List of all available monitors
        /// </summary>
        public List<MonitorInfo> Monitors { get; set; } = new();

        /// <summary>
        /// Combined bounds of all monitors
        /// </summary>
        public Rectangle TotalBounds { get; set; } = new();

        /// <summary>
        /// Primary monitor index
        /// </summary>
        public int PrimaryIndex { get; set; }

        /// <summary>
        /// Get the primary monitor
        /// </summary>
        public MonitorInfo? PrimaryMonitor => 
            PrimaryIndex < Monitors.Count ? Monitors[PrimaryIndex] : null;

        /// <summary>
        /// Get monitor at a specific point
        /// </summary>
        public MonitorInfo? GetMonitorAtPoint(int x, int y)
        {
            return Monitors.Find(m => m.Bounds.Contains(x, y));
        }
    }

    /// <summary>
    /// Capture mode for multi-monitor scenarios
    /// </summary>
    public enum CaptureMode
    {
        /// <summary>
        /// Capture a single monitor by index
        /// </summary>
        SingleMonitor,

        /// <summary>
        /// Capture the primary monitor
        /// </summary>
        PrimaryMonitor,

        /// <summary>
        /// Capture all monitors as one combined image
        /// </summary>
        AllMonitors,

        /// <summary>
        /// Capture specific monitors by indices
        /// </summary>
        SelectedMonitors,

        /// <summary>
        /// Capture the monitor containing a specific window
        /// </summary>
        WindowMonitor
    }

    /// <summary>
    /// Screen capture configuration
    /// </summary>
    public class CaptureConfiguration
    {
        /// <summary>
        /// Capture mode
        /// </summary>
        public CaptureMode Mode { get; set; } = CaptureMode.PrimaryMonitor;

        /// <summary>
        /// Monitor indices for SingleMonitor or SelectedMonitors mode
        /// </summary>
        public List<int> MonitorIndices { get; set; } = new();

        /// <summary>
        /// Window identifier for WindowMonitor mode
        /// </summary>
        public string? WindowId { get; set; }

        /// <summary>
        /// Target frames per second
        /// </summary>
        public int TargetFps { get; set; } = 30;

        /// <summary>
        /// Whether to capture cursor
        /// </summary>
        public bool CaptureCursor { get; set; } = true;

        /// <summary>
        /// Capture region within the selected monitor(s)
        /// </summary>
        public Rectangle? CaptureRegion { get; set; }

        /// <summary>
        /// Quality settings
        /// </summary>
        public CaptureQuality Quality { get; set; } = new();
    }

    /// <summary>
    /// Quality settings for capture
    /// </summary>
    public class CaptureQuality
    {
        /// <summary>
        /// Enable hardware acceleration if available
        /// </summary>
        public bool UseHardwareAcceleration { get; set; } = true;

        /// <summary>
        /// Color depth (16, 24, or 32 bits)
        /// </summary>
        public int ColorDepth { get; set; } = 32;

        /// <summary>
        /// Enable frame differencing for optimization
        /// </summary>
        public bool EnableFrameDiff { get; set; } = true;

        /// <summary>
        /// JPEG quality for compression (0-100)
        /// </summary>
        public int JpegQuality { get; set; } = 85;

        /// <summary>
        /// Scale factor for resolution reduction (1.0 = full resolution)
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;
    }
}