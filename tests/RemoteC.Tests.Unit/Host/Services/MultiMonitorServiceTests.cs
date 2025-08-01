using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RemoteC.Host.Services;
using RemoteC.Shared.Models;
using RemoteC.Core.Interop;

namespace RemoteC.Tests.Unit.Host.Services
{
    /// <summary>
    /// Tests for Host-side multi-monitor service functionality
    /// </summary>
    public class MultiMonitorServiceTests
    {
        private readonly Mock<IRemoteControlProvider> _mockProvider;
        private readonly Mock<ILogger<ScreenCaptureService>> _mockLogger;
        private readonly ScreenCaptureService _screenCaptureService;

        public MultiMonitorServiceTests()
        {
            _mockProvider = new Mock<IRemoteControlProvider>();
            _mockLogger = new Mock<ILogger<ScreenCaptureService>>();
            
            _screenCaptureService = new ScreenCaptureService(
                _mockProvider.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetMonitors_ShouldReturnAllAvailableMonitors()
        {
            // Arrange
            var expectedMonitors = new List<MonitorInfo>
            {
                new MonitorInfo
                {
                    Id = "\\\\.\DISPLAY1",
                    Name = "Generic PnP Monitor",
                    IsPrimary = true,
                    Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 }
                },
                new MonitorInfo
                {
                    Id = "\\\\.\DISPLAY2",
                    Name = "Dell U2415",
                    IsPrimary = false,
                    Bounds = new Rectangle { X = 1920, Y = 0, Width = 1920, Height = 1200 }
                }
            };

            _mockProvider.Setup(x => x.GetMonitorsAsync())
                .ReturnsAsync(expectedMonitors);

            // Act
            var monitors = await _screenCaptureService.GetMonitorsAsync();

            // Assert
            Assert.NotNull(monitors);
            Assert.Equal(2, monitors.Count());
            
            var primary = monitors.First(m => m.IsPrimary);
            Assert.Equal("Generic PnP Monitor", primary.Name);
        }

        [Fact]
        public async Task SelectMonitor_ShouldUpdateCaptureTarget()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var monitorId = "\\\\.\DISPLAY2";
            var monitor = new MonitorInfo
            {
                Id = monitorId,
                Bounds = new Rectangle { X = 1920, Y = 0, Width = 1920, Height = 1200 }
            };

            _mockProvider.Setup(x => x.GetMonitorByIdAsync(monitorId))
                .ReturnsAsync(monitor);

            // Act
            var result = await _screenCaptureService.SelectMonitorForSessionAsync(sessionId, monitorId);

            // Assert
            Assert.True(result);
            
            // Verify capture uses selected monitor
            _mockProvider.Setup(x => x.CaptureScreenAsync())
                .ReturnsAsync(new ScreenFrame 
                { 
                    Width = monitor.Bounds.Width,
                    Height = monitor.Bounds.Height,
                    Data = new byte[monitor.Bounds.Width * monitor.Bounds.Height * 4]
                });

            var frame = await _screenCaptureService.CaptureAsync();
            Assert.Equal(1920, frame.Width);
            Assert.Equal(1200, frame.Height);
        }

        [Fact]
        public async Task CaptureMultipleMonitors_ShouldCombineFrames()
        {
            // Arrange
            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo 
                { 
                    Id = "Monitor1",
                    Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 }
                },
                new MonitorInfo 
                { 
                    Id = "Monitor2",
                    Bounds = new Rectangle { X = 1920, Y = 0, Width = 1920, Height = 1080 }
                }
            };

            _mockProvider.Setup(x => x.GetMonitorsAsync())
                .ReturnsAsync(monitors);
            
            _mockProvider.Setup(x => x.CaptureAllMonitorsAsync())
                .ReturnsAsync(new ScreenFrame
                {
                    Width = 3840, // Combined width
                    Height = 1080,
                    Data = new byte[3840 * 1080 * 4]
                });

            // Act
            var frame = await _screenCaptureService.CaptureAllMonitorsAsync();

            // Assert
            Assert.NotNull(frame);
            Assert.Equal(3840, frame.Width);
            Assert.Equal(1080, frame.Height);
        }

        [Fact]
        public async Task HandleMonitorDisconnect_ShouldFallbackToPrimary()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var disconnectedMonitorId = "\\\\.\DISPLAY2";
            var primaryMonitor = new MonitorInfo
            {
                Id = "\\\\.\DISPLAY1",
                IsPrimary = true,
                Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 }
            };

            // First select the secondary monitor
            await _screenCaptureService.SelectMonitorForSessionAsync(sessionId, disconnectedMonitorId);

            // Simulate monitor disconnect
            _mockProvider.Setup(x => x.GetMonitorByIdAsync(disconnectedMonitorId))
                .ReturnsAsync((MonitorInfo?)null);
            
            _mockProvider.Setup(x => x.GetMonitorsAsync())
                .ReturnsAsync(new[] { primaryMonitor });

            // Act
            await _screenCaptureService.HandleMonitorConfigurationChangeAsync();
            var currentMonitor = await _screenCaptureService.GetSelectedMonitorAsync(sessionId);

            // Assert
            Assert.NotNull(currentMonitor);
            Assert.Equal(primaryMonitor.Id, currentMonitor.Id);
        }

        [Fact]
        public async Task CaptureRegion_SpanningMonitors_ShouldCaptureCorrectArea()
        {
            // Arrange
            var region = new CaptureRegion
            {
                X = 1820, // Starts 100px before end of first monitor
                Y = 100,
                Width = 200, // Spans 100px into second monitor
                Height = 300
            };

            _mockProvider.Setup(x => x.CaptureRegionAsync(It.IsAny<Rectangle>()))
                .ReturnsAsync((Rectangle r) => new ScreenFrame
                {
                    Width = r.Width,
                    Height = r.Height,
                    Data = new byte[r.Width * r.Height * 4]
                });

            // Act
            var frame = await _screenCaptureService.CaptureRegionAsync(region);

            // Assert
            Assert.NotNull(frame);
            Assert.Equal(200, frame.Width);
            Assert.Equal(300, frame.Height);
        }

        [Fact]
        public async Task GetMonitorForWindow_ShouldReturnContainingMonitor()
        {
            // Arrange
            var windowHandle = new IntPtr(12345);
            var windowBounds = new Rectangle { X = 1950, Y = 50, Width = 800, Height = 600 };
            var expectedMonitor = new MonitorInfo
            {
                Id = "\\\\.\DISPLAY2",
                Bounds = new Rectangle { X = 1920, Y = 0, Width = 1920, Height = 1200 }
            };

            _mockProvider.Setup(x => x.GetWindowBoundsAsync(windowHandle))
                .ReturnsAsync(windowBounds);
            
            _mockProvider.Setup(x => x.GetMonitorAtPointAsync(
                    windowBounds.X + windowBounds.Width / 2,
                    windowBounds.Y + windowBounds.Height / 2))
                .ReturnsAsync(expectedMonitor);

            // Act
            var monitor = await _screenCaptureService.GetMonitorForWindowAsync(windowHandle);

            // Assert
            Assert.NotNull(monitor);
            Assert.Equal("\\\\.\DISPLAY2", monitor.Id);
        }

        [Fact]
        public async Task MonitorDPIChange_ShouldUpdateScaleFactor()
        {
            // Arrange
            var monitorId = "\\\\.\DISPLAY1";
            var monitor = new MonitorInfo
            {
                Id = monitorId,
                ScaleFactor = 1.0f,
                Bounds = new Rectangle { Width = 1920, Height = 1080 }
            };

            _mockProvider.Setup(x => x.GetMonitorByIdAsync(monitorId))
                .ReturnsAsync(monitor);

            // Simulate DPI change
            monitor.ScaleFactor = 1.5f;
            _mockProvider.Setup(x => x.HandleDPIChangeAsync(monitorId))
                .Callback(() => 
                {
                    // Update logical size based on new DPI
                    monitor.Bounds.Width = (int)(1920 / 1.5f);
                    monitor.Bounds.Height = (int)(1080 / 1.5f);
                })
                .Returns(Task.CompletedTask);

            // Act
            await _screenCaptureService.HandleDPIChangeAsync(monitorId);
            var updatedMonitor = await _mockProvider.Object.GetMonitorByIdAsync(monitorId);

            // Assert
            Assert.NotNull(updatedMonitor);
            Assert.Equal(1.5f, updatedMonitor.ScaleFactor);
            Assert.Equal(1280, updatedMonitor.Bounds.Width); // 1920 / 1.5
            Assert.Equal(720, updatedMonitor.Bounds.Height); // 1080 / 1.5
        }

        [Fact]
        public async Task GetVirtualDesktopBounds_ShouldEncompassAllMonitors()
        {
            // Arrange
            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo 
                { 
                    Bounds = new Rectangle { X = -1920, Y = 0, Width = 1920, Height = 1080 }
                },
                new MonitorInfo 
                { 
                    Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 }
                },
                new MonitorInfo 
                { 
                    Bounds = new Rectangle { X = 1920, Y = -200, Width = 1920, Height = 1200 }
                }
            };

            _mockProvider.Setup(x => x.GetMonitorsAsync())
                .ReturnsAsync(monitors);

            // Act
            var bounds = await _screenCaptureService.GetVirtualDesktopBoundsAsync();

            // Assert
            Assert.NotNull(bounds);
            Assert.Equal(-1920, bounds.X); // Leftmost
            Assert.Equal(-200, bounds.Y); // Topmost
            Assert.Equal(5760, bounds.Width); // Total width from -1920 to 3840
            Assert.Equal(1200, bounds.Height); // Max height including offset
        }
    }

    /// <summary>
    /// Capture region for testing
    /// </summary>
    public class CaptureRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}