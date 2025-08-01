using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Tests.Services
{
    /// <summary>
    /// TDD test suite for multi-monitor support functionality
    /// </summary>
    public class MultiMonitorServiceTests
    {
        private readonly Mock<ILogger<MonitorService>> _mockLogger;
        private readonly Mock<IRemoteControlService> _mockRemoteControlService;
        private readonly MonitorService _monitorService;

        public MultiMonitorServiceTests()
        {
            _mockLogger = new Mock<ILogger<MonitorService>>();
            _mockRemoteControlService = new Mock<IRemoteControlService>();
            
            _monitorService = new MonitorService(
                _mockRemoteControlService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetMonitors_ShouldReturnAllConnectedMonitors()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var expectedMonitors = new List<MonitorInfo>
            {
                new MonitorInfo
                {
                    Id = "Monitor1",
                    Name = "Primary Display",
                    IsPrimary = true,
                    Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 },
                    ScaleFactor = 1.0f,
                    RefreshRate = 60
                },
                new MonitorInfo
                {
                    Id = "Monitor2",
                    Name = "Secondary Display",
                    IsPrimary = false,
                    Bounds = new Rectangle { X = 1920, Y = 0, Width = 2560, Height = 1440 },
                    ScaleFactor = 1.25f,
                    RefreshRate = 144
                }
            };

            _mockRemoteControlService.Setup(x => x.GetMonitorsAsync(deviceId))
                .ReturnsAsync(expectedMonitors);

            // Act
            var result = await _monitorService.GetMonitorsAsync(deviceId);

            // Assert
            Assert.NotNull(result);
            var monitors = result.ToList();
            Assert.Equal(2, monitors.Count);
            
            var primary = monitors.First(m => m.IsPrimary);
            Assert.Equal("Primary Display", primary.Name);
            Assert.Equal(1920, primary.Bounds.Width);
            Assert.Equal(1080, primary.Bounds.Height);
            
            var secondary = monitors.First(m => !m.IsPrimary);
            Assert.Equal("Secondary Display", secondary.Name);
            Assert.Equal(2560, secondary.Bounds.Width);
            Assert.Equal(1440, secondary.Bounds.Height);
        }

        [Fact]
        public async Task GetMonitors_WithNoMonitors_ShouldReturnEmpty()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            _mockRemoteControlService.Setup(x => x.GetMonitorsAsync(deviceId))
                .ReturnsAsync(new List<MonitorInfo>());

            // Act
            var result = await _monitorService.GetMonitorsAsync(deviceId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SelectMonitor_ValidMonitor_ShouldSucceed()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var monitorId = "Monitor1";
            
            _mockRemoteControlService.Setup(x => x.SelectMonitorAsync(sessionId, monitorId))
                .ReturnsAsync(true);

            // Act
            var result = await _monitorService.SelectMonitorAsync(sessionId, monitorId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(monitorId, result.SelectedMonitorId);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task SelectMonitor_InvalidMonitor_ShouldFail()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var monitorId = "InvalidMonitor";
            
            _mockRemoteControlService.Setup(x => x.SelectMonitorAsync(sessionId, monitorId))
                .ReturnsAsync(false);

            // Act
            var result = await _monitorService.SelectMonitorAsync(sessionId, monitorId);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.SelectedMonitorId);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public async Task SelectMonitor_ShouldUpdateSessionMonitorState()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var monitorId = "Monitor2";
            var monitorInfo = new MonitorInfo
            {
                Id = monitorId,
                Name = "Secondary Display",
                Bounds = new Rectangle { Width = 2560, Height = 1440 }
            };

            _mockRemoteControlService.Setup(x => x.SelectMonitorAsync(sessionId, monitorId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.GetSelectedMonitorAsync(sessionId))
                .ReturnsAsync(monitorInfo);

            // Act
            var result = await _monitorService.SelectMonitorAsync(sessionId, monitorId);
            var selectedMonitor = await _monitorService.GetSelectedMonitorAsync(sessionId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(selectedMonitor);
            Assert.Equal(monitorId, selectedMonitor.Id);
            Assert.Equal(2560, selectedMonitor.Bounds.Width);
            Assert.Equal(1440, selectedMonitor.Bounds.Height);
        }

        [Fact]
        public async Task GetVirtualDesktop_ShouldReturnCombinedBounds()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 } },
                new MonitorInfo { Bounds = new Rectangle { X = 1920, Y = 0, Width = 2560, Height = 1440 } },
                new MonitorInfo { Bounds = new Rectangle { X = -1920, Y = 0, Width = 1920, Height = 1080 } }
            };

            _mockRemoteControlService.Setup(x => x.GetMonitorsAsync(deviceId))
                .ReturnsAsync(monitors);

            // Act
            var result = await _monitorService.GetVirtualDesktopBoundsAsync(deviceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-1920, result.X); // Leftmost monitor
            Assert.Equal(0, result.Y);
            Assert.Equal(6400, result.Width); // Total width: 1920 + 1920 + 2560
            Assert.Equal(1440, result.Height); // Maximum height
        }

        [Fact]
        public async Task SwitchToMonitor_DuringActiveSession_ShouldNotifyClients()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var fromMonitorId = "Monitor1";
            var toMonitorId = "Monitor2";
            var notificationSent = false;

            _mockRemoteControlService.Setup(x => x.SelectMonitorAsync(sessionId, toMonitorId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.NotifyMonitorChangeAsync(sessionId, fromMonitorId, toMonitorId))
                .Callback(() => notificationSent = true)
                .Returns(Task.CompletedTask);

            // Act
            await _monitorService.SwitchMonitorAsync(sessionId, fromMonitorId, toMonitorId);

            // Assert
            Assert.True(notificationSent);
            _mockRemoteControlService.Verify(x => x.NotifyMonitorChangeAsync(sessionId, fromMonitorId, toMonitorId), Times.Once);
        }

        [Fact]
        public async Task GetMonitorAtPoint_ShouldReturnCorrectMonitor()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { Id = "Monitor1", Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 } },
                new MonitorInfo { Id = "Monitor2", Bounds = new Rectangle { X = 1920, Y = 0, Width = 2560, Height = 1440 } }
            };

            _mockRemoteControlService.Setup(x => x.GetMonitorsAsync(deviceId))
                .ReturnsAsync(monitors);

            // Act
            var monitor1 = await _monitorService.GetMonitorAtPointAsync(deviceId, 960, 540);
            var monitor2 = await _monitorService.GetMonitorAtPointAsync(deviceId, 2500, 700);
            var noMonitor = await _monitorService.GetMonitorAtPointAsync(deviceId, -100, -100);

            // Assert
            Assert.NotNull(monitor1);
            Assert.Equal("Monitor1", monitor1.Id);
            
            Assert.NotNull(monitor2);
            Assert.Equal("Monitor2", monitor2.Id);
            
            Assert.Null(noMonitor);
        }

        [Fact]
        public async Task HandleMonitorConfigurationChange_ShouldUpdateMonitorList()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var initialMonitors = new List<MonitorInfo>
            {
                new MonitorInfo { Id = "Monitor1", Name = "Display 1" }
            };
            var updatedMonitors = new List<MonitorInfo>
            {
                new MonitorInfo { Id = "Monitor1", Name = "Display 1" },
                new MonitorInfo { Id = "Monitor2", Name = "Display 2" }
            };

            _mockRemoteControlService.SetupSequence(x => x.GetMonitorsAsync(deviceId))
                .ReturnsAsync(initialMonitors)
                .ReturnsAsync(updatedMonitors);

            // Act
            var initial = await _monitorService.GetMonitorsAsync(deviceId);
            await _monitorService.HandleMonitorConfigurationChangeAsync(deviceId);
            var updated = await _monitorService.GetMonitorsAsync(deviceId);

            // Assert
            Assert.Single(initial);
            Assert.Equal(2, updated.Count());
        }

        [Fact]
        public async Task CaptureSpecificMonitor_ShouldOnlyCaptureSelectedArea()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var monitorId = "Monitor2";
            var expectedBounds = new ScreenBounds { X = 1920, Y = 0, Width = 2560, Height = 1440 };

            _mockRemoteControlService.Setup(x => x.SelectMonitorAsync(sessionId, monitorId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.GetMonitorBoundsAsync(sessionId, monitorId))
                .ReturnsAsync(expectedBounds);

            // Act
            await _monitorService.SelectMonitorAsync(sessionId, monitorId);
            var captureBounds = await _monitorService.GetCaptureBoundsAsync(sessionId);

            // Assert
            Assert.NotNull(captureBounds);
            Assert.Equal(expectedBounds.X, captureBounds.X);
            Assert.Equal(expectedBounds.Y, captureBounds.Y);
            Assert.Equal(expectedBounds.Width, captureBounds.Width);
            Assert.Equal(expectedBounds.Height, captureBounds.Height);
        }

        [Fact]
        public async Task GetPrimaryMonitor_ShouldReturnPrimaryDisplay()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var monitors = new List<MonitorInfo>
            {
                new MonitorInfo { Id = "Monitor1", IsPrimary = false },
                new MonitorInfo { Id = "Monitor2", IsPrimary = true },
                new MonitorInfo { Id = "Monitor3", IsPrimary = false }
            };

            _mockRemoteControlService.Setup(x => x.GetMonitorsAsync(deviceId))
                .ReturnsAsync(monitors);

            // Act
            var primary = await _monitorService.GetPrimaryMonitorAsync(deviceId);

            // Assert
            Assert.NotNull(primary);
            Assert.Equal("Monitor2", primary.Id);
            Assert.True(primary.IsPrimary);
        }

        [Fact]
        public async Task TranslateCoordinates_BetweenMonitors_ShouldCalculateCorrectly()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var fromMonitor = new MonitorInfo { Id = "Monitor1", Bounds = new Rectangle { X = 0, Y = 0, Width = 1920, Height = 1080 } };
            var toMonitor = new MonitorInfo { Id = "Monitor2", Bounds = new Rectangle { X = 1920, Y = 0, Width = 2560, Height = 1440 } };

            // Coordinates in Monitor1 space
            var localX = 1900;
            var localY = 500;

            // Act
            var globalCoords = _monitorService.ToGlobalCoordinates(fromMonitor, localX, localY);
            var monitor2Coords = _monitorService.ToMonitorCoordinates(toMonitor, globalCoords.X, globalCoords.Y);

            // Assert
            Assert.Equal(1900, globalCoords.X); // Global X = Monitor1.Bounds.X + localX
            Assert.Equal(500, globalCoords.Y);
            Assert.Equal(-20, monitor2Coords.X); // Local to Monitor2 = globalX - Monitor2.Bounds.X
            Assert.Equal(500, monitor2Coords.Y);
        }
    }
}