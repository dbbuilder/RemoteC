using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Host.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Host.Tests.Services
{
    public class ClipboardManagerTests
    {
        private readonly Mock<ILogger<ClipboardManager>> _mockLogger;
        private readonly Mock<IClipboardAccess> _mockClipboardAccess;
        private readonly ClipboardManager _manager;

        public ClipboardManagerTests()
        {
            _mockLogger = new Mock<ILogger<ClipboardManager>>();
            _mockClipboardAccess = new Mock<IClipboardAccess>();
            _manager = new ClipboardManager(_mockLogger.Object, _mockClipboardAccess.Object);
        }

        [Fact]
        public async Task GetClipboardContent_TextInClipboard_ShouldReturnTextContent()
        {
            // Arrange
            var expectedText = "Test clipboard text";
            _mockClipboardAccess.Setup(x => x.GetTextAsync())
                .ReturnsAsync(expectedText);
            _mockClipboardAccess.Setup(x => x.ContainsText())
                .Returns(true);

            // Act
            var result = await _manager.GetClipboardContentAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ClipboardContentType.Text, result.Type);
            Assert.Equal(expectedText, result.Text);
            Assert.Equal(expectedText.Length, result.Size);
        }

        [Fact]
        public async Task GetClipboardContent_ImageInClipboard_ShouldReturnImageContent()
        {
            // Arrange
            var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            _mockClipboardAccess.Setup(x => x.GetImageAsync())
                .ReturnsAsync(imageData);
            _mockClipboardAccess.Setup(x => x.ContainsImage())
                .Returns(true);
            _mockClipboardAccess.Setup(x => x.ContainsText())
                .Returns(false);

            // Act
            var result = await _manager.GetClipboardContentAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ClipboardContentType.Image, result.Type);
            Assert.Equal(imageData, result.ImageData);
            Assert.Equal("PNG", result.ImageFormat);
            Assert.Equal(imageData.Length, result.Size);
        }

        [Fact]
        public async Task GetClipboardContent_FilesInClipboard_ShouldReturnFileList()
        {
            // Arrange
            var files = new[]
            {
                @"C:\Users\test\document.docx",
                @"C:\Users\test\image.png"
            };
            _mockClipboardAccess.Setup(x => x.GetFileDropListAsync())
                .ReturnsAsync(files);
            _mockClipboardAccess.Setup(x => x.ContainsFileDropList())
                .Returns(true);
            _mockClipboardAccess.Setup(x => x.ContainsText())
                .Returns(false);
            _mockClipboardAccess.Setup(x => x.ContainsImage())
                .Returns(false);

            // Act
            var result = await _manager.GetClipboardContentAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ClipboardContentType.FileList, result.Type);
            Assert.Equal(2, result.Files.Length);
            Assert.Equal(files[0], result.Files[0].Path);
            Assert.Equal(files[1], result.Files[1].Path);
        }

        [Fact]
        public async Task GetClipboardContent_EmptyClipboard_ShouldReturnNull()
        {
            // Arrange
            _mockClipboardAccess.Setup(x => x.ContainsText()).Returns(false);
            _mockClipboardAccess.Setup(x => x.ContainsImage()).Returns(false);
            _mockClipboardAccess.Setup(x => x.ContainsFileDropList()).Returns(false);

            // Act
            var result = await _manager.GetClipboardContentAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetClipboardContent_TextContent_ShouldSetText()
        {
            // Arrange
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "New clipboard text"
            };

            _mockClipboardAccess.Setup(x => x.SetTextAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _manager.SetClipboardContentAsync(content);

            // Assert
            Assert.True(result);
            _mockClipboardAccess.Verify(x => x.SetTextAsync("New clipboard text"), Times.Once);
        }

        [Fact]
        public async Task SetClipboardContent_ImageContent_ShouldSetImage()
        {
            // Arrange
            var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Image,
                ImageData = imageData,
                ImageFormat = "PNG"
            };

            _mockClipboardAccess.Setup(x => x.SetImageAsync(It.IsAny<byte[]>()))
                .ReturnsAsync(true);

            // Act
            var result = await _manager.SetClipboardContentAsync(content);

            // Assert
            Assert.True(result);
            _mockClipboardAccess.Verify(x => x.SetImageAsync(imageData), Times.Once);
        }

        [Fact]
        public async Task MonitorClipboard_ContentChanges_ShouldRaiseEvent()
        {
            // Arrange
            var tcs = new TaskCompletionSource<ClipboardContent>();
            ClipboardContent? capturedContent = null;
            
            _manager.ClipboardChanged += (sender, args) =>
            {
                capturedContent = args.Content;
                tcs.SetResult(args.Content);
            };

            var newContent = new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Changed text"
            };

            _mockClipboardAccess.Setup(x => x.ContainsText()).Returns(true);
            _mockClipboardAccess.Setup(x => x.GetTextAsync()).ReturnsAsync("Changed text");

            // Act
            await _manager.StartMonitoringAsync(500);
            
            // Simulate clipboard change
            _mockClipboardAccess.Raise(x => x.ClipboardChanged += null, EventArgs.Empty);
            
            // Wait for event with timeout
            var result = await Task.WhenAny(tcs.Task, Task.Delay(1000));

            // Assert
            Assert.Equal(tcs.Task, result);
            Assert.NotNull(capturedContent);
            Assert.Equal("Changed text", capturedContent.Text);

            await _manager.StopMonitoringAsync();
        }

        [Fact]
        public async Task ClearClipboard_ShouldClearAllContent()
        {
            // Arrange
            _mockClipboardAccess.Setup(x => x.ClearAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _manager.ClearClipboardAsync();

            // Assert
            Assert.True(result);
            _mockClipboardAccess.Verify(x => x.ClearAsync(), Times.Once);
        }

        [Fact]
        public async Task GetClipboardHistory_ShouldReturnRecentItems()
        {
            // Arrange
            // First, add some items to history by simulating clipboard changes
            var items = new[]
            {
                new ClipboardContent { Type = ClipboardContentType.Text, Text = "Item 1" },
                new ClipboardContent { Type = ClipboardContentType.Text, Text = "Item 2" },
                new ClipboardContent { Type = ClipboardContentType.Text, Text = "Item 3" }
            };

            foreach (var item in items)
            {
                await _manager.AddToHistoryAsync(item);
            }

            // Act
            var history = await _manager.GetHistoryAsync(2);

            // Assert
            Assert.NotNull(history);
            Assert.Equal(2, history.Count);
            Assert.Equal("Item 3", history[0].Text); // Most recent first
            Assert.Equal("Item 2", history[1].Text);
        }

        [Fact]
        public async Task SetClipboardContent_HtmlContent_ShouldSetHtml()
        {
            // Arrange
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Html,
                Html = "<p>Hello <b>World</b></p>",
                Text = "Hello World" // Plain text fallback
            };

            _mockClipboardAccess.Setup(x => x.SetHtmlAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _manager.SetClipboardContentAsync(content);

            // Assert
            Assert.True(result);
            _mockClipboardAccess.Verify(x => x.SetHtmlAsync(content.Html, content.Text), Times.Once);
        }

        [Fact]
        public async Task IsFormatSupported_ShouldCheckPlatformSupport()
        {
            // Arrange
            _mockClipboardAccess.Setup(x => x.IsFormatSupported(ClipboardContentType.Text))
                .Returns(true);
            _mockClipboardAccess.Setup(x => x.IsFormatSupported(ClipboardContentType.FileList))
                .Returns(false);

            // Act
            var textSupported = _manager.IsFormatSupported(ClipboardContentType.Text);
            var fileListSupported = _manager.IsFormatSupported(ClipboardContentType.FileList);

            // Assert
            Assert.True(textSupported);
            Assert.False(fileListSupported);
        }

        [Fact]
        public async Task GetClipboardContent_WithSizeLimit_ShouldTruncateLargeContent()
        {
            // Arrange
            var largeText = new string('A', 20 * 1024 * 1024); // 20MB
            _mockClipboardAccess.Setup(x => x.GetTextAsync())
                .ReturnsAsync(largeText);
            _mockClipboardAccess.Setup(x => x.ContainsText())
                .Returns(true);

            _manager.MaxContentSize = 10 * 1024 * 1024; // 10MB limit

            // Act
            var result = await _manager.GetClipboardContentAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsTruncated);
            Assert.Equal(10 * 1024 * 1024, result.Text.Length);
        }

        [Fact]
        public async Task MonitorClipboard_RapidChanges_ShouldThrottle()
        {
            // Arrange
            var changeCount = 0;
            _manager.ClipboardChanged += (sender, args) => changeCount++;

            _mockClipboardAccess.Setup(x => x.ContainsText()).Returns(true);
            _mockClipboardAccess.Setup(x => x.GetTextAsync())
                .ReturnsAsync(() => $"Text {changeCount}");

            // Act
            await _manager.StartMonitoringAsync(100);

            // Simulate rapid clipboard changes
            for (int i = 0; i < 10; i++)
            {
                _mockClipboardAccess.Raise(x => x.ClipboardChanged += null, EventArgs.Empty);
                await Task.Delay(10); // Very rapid changes
            }

            await Task.Delay(200); // Wait for throttling
            await _manager.StopMonitoringAsync();

            // Assert
            Assert.True(changeCount < 10); // Should have throttled some changes
        }
    }
}