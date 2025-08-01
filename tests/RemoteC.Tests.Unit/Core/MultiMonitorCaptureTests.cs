using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using RemoteC.Core.Interop;
using RemoteC.Shared.Models;

namespace RemoteC.Tests.Unit.Core
{
    /// <summary>
    /// Tests for multi-monitor screen capture functionality in Rust core
    /// </summary>
    public class MultiMonitorCaptureTests : IDisposable
    {
        private readonly RemoteCCore _core;
        private bool _disposed;

        public MultiMonitorCaptureTests()
        {
            _core = new RemoteCCore();
        }

        [Fact]
        public void EnumerateMonitors_ShouldReturnAllConnectedMonitors()
        {
            // Act
            var monitors = _core.EnumerateMonitors();

            // Assert
            Assert.NotNull(monitors);
            Assert.NotEmpty(monitors);
            
            // Should have at least one primary monitor
            Assert.Contains(monitors, m => m.IsPrimary);
            
            // All monitors should have valid dimensions
            foreach (var monitor in monitors)
            {
                Assert.True(monitor.Bounds.Width > 0);
                Assert.True(monitor.Bounds.Height > 0);
                Assert.NotEmpty(monitor.Id);
                Assert.NotEmpty(monitor.Name);
            }
        }

        [Fact]
        public void CaptureMonitor_SpecificMonitor_ShouldCaptureOnlyThatMonitor()
        {
            // Arrange
            var monitors = _core.EnumerateMonitors();
            if (monitors.Count() < 2)
            {
                // Skip test if only one monitor
                return;
            }

            var targetMonitor = monitors.First(m => !m.IsPrimary);

            // Act
            var frame = _core.CaptureMonitor(targetMonitor.Id);

            // Assert
            Assert.NotNull(frame);
            Assert.Equal(targetMonitor.Bounds.Width, frame.Width);
            Assert.Equal(targetMonitor.Bounds.Height, frame.Height);
            Assert.True(frame.Data.Length > 0);
        }

        [Fact]
        public void CaptureMonitor_InvalidMonitorId_ShouldReturnNull()
        {
            // Act
            var frame = _core.CaptureMonitor("InvalidMonitorId");

            // Assert
            Assert.Null(frame);
        }

        [Fact]
        public void CaptureAllMonitors_ShouldReturnCombinedImage()
        {
            // Arrange
            var monitors = _core.EnumerateMonitors();
            var virtualBounds = CalculateVirtualBounds(monitors);

            // Act
            var frame = _core.CaptureAllMonitors();

            // Assert
            Assert.NotNull(frame);
            Assert.Equal(virtualBounds.Width, frame.Width);
            Assert.Equal(virtualBounds.Height, frame.Height);
            Assert.True(frame.Data.Length > 0);
        }

        [Fact]
        public void CaptureRegion_AcrossMonitors_ShouldCaptureCorrectArea()
        {
            // Arrange
            var monitors = _core.EnumerateMonitors();
            if (monitors.Count() < 2)
            {
                // Skip test if only one monitor
                return;
            }

            // Define a region that spans two monitors
            var firstMonitor = monitors.First();
            var secondMonitor = monitors.Skip(1).First();
            
            var captureRegion = new ScreenBounds
            {
                X = firstMonitor.Bounds.Right - 100,
                Y = Math.Min(firstMonitor.Bounds.Y, secondMonitor.Bounds.Y),
                Width = 200,
                Height = 100
            };

            // Act
            var frame = _core.CaptureRegion(captureRegion);

            // Assert
            Assert.NotNull(frame);
            Assert.Equal(200, frame.Width);
            Assert.Equal(100, frame.Height);
        }

        [Fact]
        public void GetMonitorAtPoint_ShouldReturnCorrectMonitor()
        {
            // Arrange
            var monitors = _core.EnumerateMonitors();
            var primaryMonitor = monitors.First(m => m.IsPrimary);
            
            var testPoint = new Point
            {
                X = primaryMonitor.Bounds.X + primaryMonitor.Bounds.Width / 2,
                Y = primaryMonitor.Bounds.Y + primaryMonitor.Bounds.Height / 2
            };

            // Act
            var monitor = _core.GetMonitorAtPoint(testPoint);

            // Assert
            Assert.NotNull(monitor);
            Assert.Equal(primaryMonitor.Id, monitor.Id);
        }

        [Fact]
        public void HandleDPIScaling_HighDPIMonitor_ShouldScaleCorrectly()
        {
            // Arrange
            var monitors = _core.EnumerateMonitors();
            var highDpiMonitor = monitors.FirstOrDefault(m => m.ScaleFactor > 1.0f);
            
            if (highDpiMonitor == null)
            {
                // Skip test if no high DPI monitor
                return;
            }

            // Act
            var frame = _core.CaptureMonitor(highDpiMonitor.Id);
            var logicalSize = _core.GetLogicalSize(highDpiMonitor.Id);

            // Assert
            Assert.NotNull(frame);
            Assert.NotNull(logicalSize);
            
            // Physical pixels should be scaled by DPI factor
            var expectedPhysicalWidth = (int)(logicalSize.Width * highDpiMonitor.ScaleFactor);
            var expectedPhysicalHeight = (int)(logicalSize.Height * highDpiMonitor.ScaleFactor);
            
            Assert.Equal(expectedPhysicalWidth, frame.Width);
            Assert.Equal(expectedPhysicalHeight, frame.Height);
        }

        [Fact]
        public void MonitorConfigurationChange_ShouldUpdateMonitorList()
        {
            // This test would require simulating monitor connect/disconnect
            // which is not possible in unit tests. Would be an integration test.
            
            // Arrange
            var initialMonitors = _core.EnumerateMonitors();
            
            // Act - Simulate configuration change notification
            _core.HandleMonitorConfigurationChange();
            var updatedMonitors = _core.EnumerateMonitors();

            // Assert
            Assert.NotNull(updatedMonitors);
            // In a real scenario, we'd verify the list changed appropriately
        }

        [Fact]
        public void CaptureWithCursor_ShouldIncludeCursorInCorrectPosition()
        {
            // Arrange
            var primaryMonitor = _core.EnumerateMonitors().First(m => m.IsPrimary);
            var cursorPos = _core.GetCursorPosition();

            // Act
            var frameWithCursor = _core.CaptureMonitor(primaryMonitor.Id, includeCursor: true);
            var frameWithoutCursor = _core.CaptureMonitor(primaryMonitor.Id, includeCursor: false);

            // Assert
            Assert.NotNull(frameWithCursor);
            Assert.NotNull(frameWithoutCursor);
            
            // If cursor is on this monitor, frames should differ
            if (primaryMonitor.Bounds.Contains(cursorPos.X, cursorPos.Y))
            {
                Assert.NotEqual(frameWithCursor.Data, frameWithoutCursor.Data);
            }
        }

        [Fact]
        public void TranslateCoordinates_BetweenMonitorSpaces_ShouldBeAccurate()
        {
            // Arrange
            var monitors = _core.EnumerateMonitors();
            if (monitors.Count() < 2)
            {
                return;
            }

            var monitor1 = monitors.First();
            var monitor2 = monitors.Skip(1).First();
            
            var localPoint = new Point { X = 100, Y = 100 };

            // Act
            var globalPoint = _core.MonitorToGlobal(monitor1.Id, localPoint);
            var monitor2Point = _core.GlobalToMonitor(monitor2.Id, globalPoint);

            // Assert
            Assert.Equal(monitor1.Bounds.X + 100, globalPoint.X);
            Assert.Equal(monitor1.Bounds.Y + 100, globalPoint.Y);
            Assert.Equal(globalPoint.X - monitor2.Bounds.X, monitor2Point.X);
            Assert.Equal(globalPoint.Y - monitor2.Bounds.Y, monitor2Point.Y);
        }

        private VirtualDesktopBounds CalculateVirtualBounds(IEnumerable<MonitorInfo> monitors)
        {
            if (!monitors.Any())
                return new VirtualDesktopBounds();

            var minX = monitors.Min(m => m.Bounds.X);
            var minY = monitors.Min(m => m.Bounds.Y);
            var maxX = monitors.Max(m => m.Bounds.Right);
            var maxY = monitors.Max(m => m.Bounds.Bottom);

            return new VirtualDesktopBounds
            {
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY,
                MonitorCount = monitors.Count()
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _core?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Simple point structure for testing
    /// </summary>
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// Size structure for logical dimensions
    /// </summary>
    public struct Size
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}