using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class ClipboardServiceTests
    {
        private readonly Mock<ILogger<ClipboardService>> _mockLogger;
        private readonly Mock<IRemoteControlService> _mockRemoteControlService;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly ClipboardService _service;

        public ClipboardServiceTests()
        {
            _mockLogger = new Mock<ILogger<ClipboardService>>();
            _mockRemoteControlService = new Mock<IRemoteControlService>();
            _mockSessionService = new Mock<ISessionService>();
            _service = new ClipboardService(
                _mockLogger.Object,
                _mockRemoteControlService.Object,
                _mockSessionService.Object);
        }

        [Fact]
        public async Task GetClipboardContent_ValidSession_ShouldReturnContent()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var expectedContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Hello, World!",
                Size = 13,
                Timestamp = DateTime.UtcNow
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.GetClipboardContentAsync(sessionId))
                .ReturnsAsync(expectedContent);

            // Act
            var result = await _service.GetClipboardContentAsync(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ClipboardContentType.Text, result.Type);
            Assert.Equal("Hello, World!", result.Text);
            Assert.Equal(13, result.Size);
        }

        [Fact]
        public async Task GetClipboardContent_InvalidSession_ShouldThrowException()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetClipboardContentAsync(sessionId));
        }

        [Fact]
        public async Task SetClipboardContent_TextContent_ShouldSucceed()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "New clipboard text",
                Size = 18
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.SetClipboardContentAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SetClipboardContentAsync(sessionId, content);

            // Assert
            Assert.True(result);
            _mockRemoteControlService.Verify(x => x.SetClipboardContentAsync(sessionId, 
                It.Is<ClipboardContent>(c => c.Text == "New clipboard text")), Times.Once);
        }

        [Fact]
        public async Task SetClipboardContent_ImageContent_ShouldSucceed()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Image,
                ImageData = imageData,
                ImageFormat = "PNG",
                Size = imageData.Length
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.SetClipboardContentAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SetClipboardContentAsync(sessionId, content);

            // Assert
            Assert.True(result);
            _mockRemoteControlService.Verify(x => x.SetClipboardContentAsync(sessionId,
                It.Is<ClipboardContent>(c => c.Type == ClipboardContentType.Image && c.ImageFormat == "PNG")), 
                Times.Once);
        }

        [Fact]
        public async Task SetClipboardContent_FileListContent_ShouldSucceed()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.FileList,
                Files = new[]
                {
                    new ClipboardFile { Path = @"C:\Users\test\document.docx", Size = 1024 },
                    new ClipboardFile { Path = @"C:\Users\test\image.png", Size = 2048 }
                },
                Size = 2
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.SetClipboardContentAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SetClipboardContentAsync(sessionId, content);

            // Assert
            Assert.True(result);
            _mockRemoteControlService.Verify(x => x.SetClipboardContentAsync(sessionId,
                It.Is<ClipboardContent>(c => c.Type == ClipboardContentType.FileList && c.Files.Length == 2)),
                Times.Once);
        }

        [Fact]
        public async Task SetClipboardContent_ExceedsMaxSize_ShouldReturnFalse()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var largeText = new string('A', 11 * 1024 * 1024); // 11MB
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = largeText,
                Size = largeText.Length
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SetClipboardContentAsync(sessionId, content);

            // Assert
            Assert.False(result);
            _mockRemoteControlService.Verify(x => x.SetClipboardContentAsync(It.IsAny<Guid>(), It.IsAny<ClipboardContent>()), 
                Times.Never);
        }

        [Fact]
        public async Task SyncClipboard_BidirectionalSync_ShouldUpdateBothSides()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var direction = ClipboardSyncDirection.Bidirectional;
            
            var hostContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Host clipboard",
                Timestamp = DateTime.UtcNow.AddSeconds(-5)
            };

            var clientContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Client clipboard",
                Timestamp = DateTime.UtcNow
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.GetHostClipboardAsync(sessionId))
                .ReturnsAsync(hostContent);
            _mockRemoteControlService.Setup(x => x.GetClientClipboardAsync(sessionId))
                .ReturnsAsync(clientContent);
            _mockRemoteControlService.Setup(x => x.SetClipboardContentAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SyncClipboardAsync(sessionId, direction);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Client clipboard", result.SyncedContent?.Text);
            Assert.Equal(ClipboardSyncDirection.ClientToHost, result.ActualDirection);
        }

        [Fact]
        public async Task SyncClipboard_HostToClientOnly_ShouldSyncOneWay()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var direction = ClipboardSyncDirection.HostToClient;
            
            var hostContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Host clipboard",
                Timestamp = DateTime.UtcNow
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.GetHostClipboardAsync(sessionId))
                .ReturnsAsync(hostContent);
            _mockRemoteControlService.Setup(x => x.SetClientClipboardAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SyncClipboardAsync(sessionId, direction);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Host clipboard", result.SyncedContent?.Text);
            Assert.Equal(ClipboardSyncDirection.HostToClient, result.ActualDirection);
        }

        [Fact]
        public async Task EnableClipboardMonitoring_ShouldStartMonitoring()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var config = new ClipboardMonitoringConfig
            {
                Enabled = true,
                PollingIntervalMs = 500,
                Direction = ClipboardSyncDirection.Bidirectional,
                MaxContentSize = 10 * 1024 * 1024
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.EnableClipboardMonitoringAsync(sessionId, config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DisableClipboardMonitoring_ShouldStopMonitoring()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DisableClipboardMonitoringAsync(sessionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetClipboardHistory_ShouldReturnRecentItems()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var history = new[]
            {
                new ClipboardHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Content = new ClipboardContent { Type = ClipboardContentType.Text, Text = "Item 1" },
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Source = ClipboardSource.Host
                },
                new ClipboardHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Content = new ClipboardContent { Type = ClipboardContentType.Text, Text = "Item 2" },
                    Timestamp = DateTime.UtcNow.AddMinutes(-2),
                    Source = ClipboardSource.Client
                }
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.GetClipboardHistoryAsync(sessionId, 10))
                .ReturnsAsync(history);

            // Act
            var result = await _service.GetClipboardHistoryAsync(sessionId, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal("Item 1", result[0].Content.Text);
            Assert.Equal(ClipboardSource.Host, result[0].Source);
        }

        [Fact]
        public async Task ClearClipboard_ShouldClearBothSides()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.ClearClipboardAsync(sessionId, ClipboardTarget.Both))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ClearClipboardAsync(sessionId, ClipboardTarget.Both);

            // Assert
            Assert.True(result);
            _mockRemoteControlService.Verify(x => x.ClearClipboardAsync(sessionId, ClipboardTarget.Both), Times.Once);
        }

        [Theory]
        [InlineData(ClipboardContentType.Text, true)]
        [InlineData(ClipboardContentType.Image, true)]
        [InlineData(ClipboardContentType.FileList, false)]
        [InlineData(ClipboardContentType.Html, true)]
        public async Task IsContentTypeSupported_ShouldReturnCorrectSupport(ClipboardContentType type, bool expectedSupport)
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);
            _mockRemoteControlService.Setup(x => x.IsClipboardTypeSupported(type))
                .Returns(expectedSupport);

            // Act
            var result = await _service.IsContentTypeSupportedAsync(sessionId, type);

            // Assert
            Assert.Equal(expectedSupport, result);
        }

        [Fact]
        public async Task HandleClipboardConflict_ShouldResolveBasedOnPolicy()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var hostContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Host content",
                Timestamp = DateTime.UtcNow
            };
            var clientContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Client content",
                Timestamp = DateTime.UtcNow.AddSeconds(-1)
            };

            _mockSessionService.Setup(x => x.ValidateSessionAsync(sessionId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ResolveClipboardConflictAsync(
                sessionId, 
                hostContent, 
                clientContent, 
                ConflictResolutionPolicy.PreferNewest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Host content", result.Text);
            Assert.Equal(ClipboardSource.Host, result.ResolvedSource);
        }
    }
}