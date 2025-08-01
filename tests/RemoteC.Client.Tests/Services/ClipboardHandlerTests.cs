using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Client.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Client.Tests.Services
{
    public class ClipboardHandlerTests
    {
        private readonly Mock<ILogger<ClipboardHandler>> _mockLogger;
        private readonly Mock<IApiClient> _mockApiClient;
        private readonly Mock<ILocalClipboard> _mockLocalClipboard;
        private readonly ClipboardHandler _handler;

        public ClipboardHandlerTests()
        {
            _mockLogger = new Mock<ILogger<ClipboardHandler>>();
            _mockApiClient = new Mock<IApiClient>();
            _mockLocalClipboard = new Mock<ILocalClipboard>();
            _handler = new ClipboardHandler(
                _mockLogger.Object,
                _mockApiClient.Object,
                _mockLocalClipboard.Object);
        }

        [Fact]
        public async Task SendClipboardToHost_TextContent_ShouldSucceed()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Client clipboard text",
                Size = 21
            };

            _mockLocalClipboard.Setup(x => x.GetContentAsync())
                .ReturnsAsync(content);
            _mockApiClient.Setup(x => x.SetHostClipboardAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.SendClipboardToHostAsync(sessionId);

            // Assert
            Assert.True(result);
            _mockApiClient.Verify(x => x.SetHostClipboardAsync(sessionId,
                It.Is<ClipboardContent>(c => c.Text == "Client clipboard text")), Times.Once);
        }

        [Fact]
        public async Task ReceiveClipboardFromHost_ShouldUpdateLocalClipboard()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var hostContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Host clipboard text"
            };

            _mockApiClient.Setup(x => x.GetHostClipboardAsync(sessionId))
                .ReturnsAsync(hostContent);
            _mockLocalClipboard.Setup(x => x.SetContentAsync(It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ReceiveClipboardFromHostAsync(sessionId);

            // Assert
            Assert.True(result);
            _mockLocalClipboard.Verify(x => x.SetContentAsync(
                It.Is<ClipboardContent>(c => c.Text == "Host clipboard text")), Times.Once);
        }

        [Fact]
        public async Task StartAutoSync_ShouldSyncPeriodically()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var syncCount = 0;
            var config = new ClipboardSyncConfig
            {
                Enabled = true,
                Direction = ClipboardSyncDirection.Bidirectional,
                IntervalMs = 100,
                MaxRetries = 3
            };

            _mockApiClient.Setup(x => x.SyncClipboardAsync(sessionId, It.IsAny<ClipboardSyncDirection>()))
                .ReturnsAsync(() => new ClipboardSyncResult 
                { 
                    Success = true,
                    SyncedContent = new ClipboardContent { Type = ClipboardContentType.Text, Text = $"Sync {++syncCount}" }
                });

            // Act
            await _handler.StartAutoSyncAsync(sessionId, config);
            await Task.Delay(350); // Wait for ~3 syncs
            await _handler.StopAutoSyncAsync(sessionId);

            // Assert
            Assert.True(syncCount >= 3);
            _mockApiClient.Verify(x => x.SyncClipboardAsync(sessionId, ClipboardSyncDirection.Bidirectional), 
                Times.AtLeast(3));
        }

        [Fact]
        public async Task HandleClipboardUpdate_FromHost_ShouldUpdateLocal()
        {
            // Arrange
            var update = new ClipboardUpdateNotification
            {
                SessionId = Guid.NewGuid(),
                Source = ClipboardSource.Host,
                Content = new ClipboardContent
                {
                    Type = ClipboardContentType.Image,
                    ImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
                    ImageFormat = "PNG"
                },
                Timestamp = DateTime.UtcNow
            };

            _mockLocalClipboard.Setup(x => x.SetContentAsync(It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            await _handler.HandleClipboardUpdateAsync(update);

            // Assert
            _mockLocalClipboard.Verify(x => x.SetContentAsync(
                It.Is<ClipboardContent>(c => c.Type == ClipboardContentType.Image)), Times.Once);
        }

        [Fact]
        public async Task HandleClipboardUpdate_FromClient_ShouldIgnore()
        {
            // Arrange
            var update = new ClipboardUpdateNotification
            {
                SessionId = Guid.NewGuid(),
                Source = ClipboardSource.Client,
                Content = new ClipboardContent
                {
                    Type = ClipboardContentType.Text,
                    Text = "Should not update"
                }
            };

            // Act
            await _handler.HandleClipboardUpdateAsync(update);

            // Assert
            _mockLocalClipboard.Verify(x => x.SetContentAsync(It.IsAny<ClipboardContent>()), Times.Never);
        }

        [Fact]
        public async Task CompressLargeContent_ShouldCompressBeforeSending()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var largeText = new string('A', 100_000); // 100KB of text
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = largeText,
                Size = largeText.Length
            };

            _mockLocalClipboard.Setup(x => x.GetContentAsync())
                .ReturnsAsync(content);
            _mockApiClient.Setup(x => x.SetHostClipboardAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.SendClipboardToHostAsync(sessionId, compress: true);

            // Assert
            Assert.True(result);
            _mockApiClient.Verify(x => x.SetHostClipboardAsync(sessionId,
                It.Is<ClipboardContent>(c => c.IsCompressed && c.CompressedData != null)), Times.Once);
        }

        [Fact]
        public async Task HandleError_DuringSync_ShouldRetry()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var attempts = 0;
            var config = new ClipboardSyncConfig
            {
                Enabled = true,
                MaxRetries = 3,
                RetryDelayMs = 50
            };

            _mockApiClient.Setup(x => x.SyncClipboardAsync(sessionId, It.IsAny<ClipboardSyncDirection>()))
                .ReturnsAsync(() =>
                {
                    attempts++;
                    if (attempts < 3)
                        throw new Exception("Network error");
                    
                    return new ClipboardSyncResult { Success = true };
                });

            // Act
            var result = await _handler.SyncWithRetryAsync(sessionId, config);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public async Task ValidateContent_BeforeSending_ShouldRejectInvalid()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var invalidContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = null // Invalid - null text
            };

            _mockLocalClipboard.Setup(x => x.GetContentAsync())
                .ReturnsAsync(invalidContent);

            // Act
            var result = await _handler.SendClipboardToHostAsync(sessionId);

            // Assert
            Assert.False(result);
            _mockApiClient.Verify(x => x.SetHostClipboardAsync(It.IsAny<Guid>(), It.IsAny<ClipboardContent>()), 
                Times.Never);
        }

        [Fact]
        public async Task ConvertFormat_UnsupportedOnHost_ShouldConvertToSupported()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var richTextContent = new ClipboardContent
            {
                Type = ClipboardContentType.RichText,
                RichText = @"{\rtf1\ansi\b Hello World\b0}",
                Text = "Hello World" // Plain text fallback
            };

            _mockLocalClipboard.Setup(x => x.GetContentAsync())
                .ReturnsAsync(richTextContent);
            _mockApiClient.Setup(x => x.IsFormatSupportedAsync(sessionId, ClipboardContentType.RichText))
                .ReturnsAsync(false);
            _mockApiClient.Setup(x => x.IsFormatSupportedAsync(sessionId, ClipboardContentType.Text))
                .ReturnsAsync(true);
            _mockApiClient.Setup(x => x.SetHostClipboardAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.SendClipboardToHostAsync(sessionId, convertUnsupported: true);

            // Assert
            Assert.True(result);
            _mockApiClient.Verify(x => x.SetHostClipboardAsync(sessionId,
                It.Is<ClipboardContent>(c => c.Type == ClipboardContentType.Text && c.Text == "Hello World")), 
                Times.Once);
        }

        [Fact]
        public async Task CacheClipboardContent_ShouldAvoidDuplicateSends()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Same content"
            };

            _mockLocalClipboard.Setup(x => x.GetContentAsync())
                .ReturnsAsync(content);
            _mockApiClient.Setup(x => x.SetHostClipboardAsync(sessionId, It.IsAny<ClipboardContent>()))
                .ReturnsAsync(true);

            // Act
            var result1 = await _handler.SendClipboardToHostAsync(sessionId);
            var result2 = await _handler.SendClipboardToHostAsync(sessionId); // Same content

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            _mockApiClient.Verify(x => x.SetHostClipboardAsync(sessionId, It.IsAny<ClipboardContent>()), 
                Times.Once); // Should only send once
        }
    }
}