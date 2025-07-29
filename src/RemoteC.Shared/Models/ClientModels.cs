using System;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Information about a remote session
    /// </summary>
    public class SessionInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public int ColorDepth { get; set; }
        public bool HasAudio { get; set; }
        public bool HasMultipleMonitors { get; set; }
        public string[] SupportedFeatures { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Defines a region of the screen
    /// </summary>
    public class ScreenRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool Contains(int x, int y)
        {
            return x >= X && x < X + Width && y >= Y && y < Y + Height;
        }

        public bool Intersects(ScreenRegion other)
        {
            return !(other.X >= X + Width || other.X + other.Width <= X ||
                    other.Y >= Y + Height || other.Y + other.Height <= Y);
        }
    }

}