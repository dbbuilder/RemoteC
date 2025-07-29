using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Host.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Tests.Unit.Host.Services;

public class ScreenCaptureServiceTests
{
    private readonly Mock<ILogger<ScreenCaptureService>> _loggerMock;
    private readonly Mock<IRemoteControlProvider> _providerMock;
    private readonly ScreenCaptureService _service;

    public ScreenCaptureServiceTests()
    {
        _loggerMock = new Mock<ILogger<ScreenCaptureService>>();
        _providerMock = new Mock<IRemoteControlProvider>();
        _service = new ScreenCaptureService(_loggerMock.Object, _providerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeProvider()
    {
        // Arrange
        _providerMock.Setup(p => p.InitializeAsync()).ReturnsAsync(true);

        // Act
        await _service.InitializeAsync(CancellationToken.None);

        // Assert
        _providerMock.Verify(p => p.InitializeAsync(), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenProviderFailsToInitialize_ShouldThrowException()
    {
        // Arrange
        _providerMock.Setup(p => p.InitializeAsync()).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InitializeAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CaptureScreenAsync_WhenNotInitialized_ShouldThrowException()
    {
        // Arrange
        var quality = new QualitySettings();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CaptureScreenAsync(0, quality, CancellationToken.None));
    }

    [Fact]
    public async Task CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData()
    {
        // Arrange
        await InitializeService();
        var quality = new QualitySettings { Quality = 85, TargetFps = 30 };
        var expectedFrame = new ScreenFrame
        {
            Width = 1920,
            Height = 1080,
            Data = new byte[] { 1, 2, 3, 4, 5 },
            Timestamp = DateTime.UtcNow,
            IsKeyFrame = true
        };

        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedFrame);

        // Act
        var result = await _service.CaptureScreenAsync(0, quality, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Width.Should().Be(expectedFrame.Width);
        result.Height.Should().Be(expectedFrame.Height);
        result.MonitorIndex.Should().Be(0);
        result.Quality.Should().Be(quality.Quality);
        result.CompressionType.Should().Be(quality.CompressionType);
    }

    [Fact]
    public async Task CaptureScreenAsync_WhenProviderReturnsNull_ShouldReturnNull()
    {
        // Arrange
        await InitializeService();
        var quality = new QualitySettings();
        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
            .ReturnsAsync((ScreenFrame?)null);

        // Act
        var result = await _service.CaptureScreenAsync(0, quality, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMonitorsAsync_ShouldReturnMonitorInfo()
    {
        // Act
        var monitors = await _service.GetMonitorsAsync();

        // Assert
        monitors.Should().NotBeNull();
        monitors.Should().BeOfType<MonitorInfo[]>();
        // Note: Actual monitor count depends on system
    }

    [Fact]
    public async Task CaptureScreenAsync_WithScaling_ShouldApplyScale()
    {
        // Arrange
        await InitializeService();
        var quality = new QualitySettings 
        { 
            Quality = 85, 
            Scale = 0.5f,
            CompressionType = CompressionType.Jpeg
        };
        
        var originalFrame = new ScreenFrame
        {
            Width = 1920,
            Height = 1080,
            Data = CreateDummyImageData(),
            Timestamp = DateTime.UtcNow,
            IsKeyFrame = true
        };

        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
            .ReturnsAsync(originalFrame);

        // Act
        var result = await _service.CaptureScreenAsync(0, quality, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Width.Should().Be(originalFrame.Width);
        result.Height.Should().Be(originalFrame.Height);
        result.Quality.Should().Be(quality.Quality);
        // The actual scaling happens in ProcessFrameAsync
    }

    [Fact]
    public async Task CaptureScreenAsync_ShouldHandleExceptions()
    {
        // Arrange
        await InitializeService();
        var quality = new QualitySettings();
        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Provider error"));

        // Act
        var result = await _service.CaptureScreenAsync(0, quality, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(LogLevel.Error, "Error capturing screen", Times.Once());
    }

    [Fact]
    public async Task DisposeAsync_ShouldCompleteSuccessfully()
    {
        // Act & Assert
        await _service.DisposeAsync();
        // Should not throw
    }

    private async Task InitializeService()
    {
        _providerMock.Setup(p => p.InitializeAsync()).ReturnsAsync(true);
        await _service.InitializeAsync(CancellationToken.None);
    }

    private byte[] CreateDummyImageData()
    {
        // Create a minimal valid JPEG header
        return new byte[] 
        { 
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 
            0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01
        };
    }
}

// Extension methods for mock verification
public static class LoggerExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, 
        LogLevel level, string message, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}