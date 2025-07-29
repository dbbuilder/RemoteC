using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Host.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Tests.Unit.Host.Services;

public class SessionManagerTests
{
    private readonly Mock<ILogger<SessionManager>> _loggerMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _loggerMock = new Mock<ILogger<SessionManager>>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _sessionManager = new SessionManager(_loggerMock.Object, _authServiceMock.Object);
    }

    [Fact]
    public async Task ValidateSessionAsync_WithValidPin_ShouldReturnTrue()
    {
        // Arrange
        var request = new SessionStartRequest
        {
            SessionId = Guid.NewGuid(),
            Pin = "123456",
            UserId = "user1"
        };

        _authServiceMock.Setup(a => a.ValidatePinAsync(request.Pin))
            .ReturnsAsync(true);
        _authServiceMock.Setup(a => a.CheckPermissionAsync(request.UserId, "session.create"))
            .ReturnsAsync(true);

        // Act
        var result = await _sessionManager.ValidateSessionAsync(request);

        // Assert
        result.Should().BeTrue();
        _authServiceMock.Verify(a => a.ValidatePinAsync(request.Pin), Times.Once);
    }

    [Fact]
    public async Task ValidateSessionAsync_WithInvalidPin_ShouldReturnFalse()
    {
        // Arrange
        var request = new SessionStartRequest
        {
            SessionId = Guid.NewGuid(),
            Pin = "000000",
            UserId = "user1"
        };

        _authServiceMock.Setup(a => a.ValidatePinAsync(request.Pin))
            .ReturnsAsync(false);

        // Act
        var result = await _sessionManager.ValidateSessionAsync(request);

        // Assert
        result.Should().BeFalse();
        _loggerMock.VerifyLog(LogLevel.Warning, "Invalid PIN", Times.Once());
    }

    [Fact]
    public async Task ValidateSessionAsync_WithoutPermission_ShouldReturnFalse()
    {
        // Arrange
        var request = new SessionStartRequest
        {
            SessionId = Guid.NewGuid(),
            UserId = "user1"
        };

        _authServiceMock.Setup(a => a.CheckPermissionAsync(request.UserId, "session.create"))
            .ReturnsAsync(false);

        // Act
        var result = await _sessionManager.ValidateSessionAsync(request);

        // Assert
        result.Should().BeFalse();
        _loggerMock.VerifyLog(LogLevel.Warning, "lacks permission", Times.Once());
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenExceptionOccurs_ShouldReturnFalse()
    {
        // Arrange
        var request = new SessionStartRequest
        {
            SessionId = Guid.NewGuid(),
            Pin = "123456"
        };

        _authServiceMock.Setup(a => a.ValidatePinAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Auth service error"));

        // Act
        var result = await _sessionManager.ValidateSessionAsync(request);

        // Assert
        result.Should().BeFalse();
        _loggerMock.VerifyLog(LogLevel.Error, "Error validating session", Times.Once());
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateSessionWithCorrectProperties()
    {
        // Arrange
        var request = new SessionStartRequest
        {
            SessionId = Guid.NewGuid(),
            UserId = "user1",
            DeviceId = "device1",
            Type = SessionType.RemoteControl,
            MonitorIndex = 1,
            InitialQuality = 90,
            TargetFps = 60
        };

        // Act
        var session = await _sessionManager.CreateSessionAsync(request);

        // Assert
        session.Should().NotBeNull();
        session.Id.Should().Be(request.SessionId);
        session.UserId.Should().Be(request.UserId);
        session.DeviceId.Should().Be(request.DeviceId);
        session.Type.Should().Be(request.Type);
        session.Status.Should().Be(SessionStatus.Active);
        session.MonitorIndex.Should().Be(request.MonitorIndex);
        session.QualitySettings.Quality.Should().Be(90);
        session.QualitySettings.TargetFps.Should().Be(60);
        session.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateSessionAsync_WithoutQualitySettings_ShouldUseDefaults()
    {
        // Arrange
        var request = new SessionStartRequest
        {
            SessionId = Guid.NewGuid(),
            UserId = "user1",
            DeviceId = "device1"
        };

        // Act
        var session = await _sessionManager.CreateSessionAsync(request);

        // Assert
        session.QualitySettings.Quality.Should().Be(85);
        session.QualitySettings.TargetFps.Should().Be(30);
    }

    [Fact]
    public async Task EndSessionAsync_WithActiveSession_ShouldMarkAsEnded()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new SessionStartRequest { SessionId = sessionId };
        await _sessionManager.CreateSessionAsync(request);

        // Act
        await _sessionManager.EndSessionAsync(sessionId);

        // Assert
        var session = await _sessionManager.GetSessionAsync(sessionId);
        session.Should().BeNull(); // Session removed from active sessions
        _loggerMock.VerifyLog(LogLevel.Information, "Ended session", Times.Once());
    }

    [Fact]
    public async Task EndSessionAsync_WithNonExistentSession_ShouldNotThrow()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act & Assert
        await _sessionManager.EndSessionAsync(sessionId);
        // Should complete without throwing
    }

    [Fact]
    public async Task GetSessionAsync_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new SessionStartRequest { SessionId = sessionId };
        var createdSession = await _sessionManager.CreateSessionAsync(request);

        // Act
        var retrievedSession = await _sessionManager.GetSessionAsync(sessionId);

        // Assert
        retrievedSession.Should().NotBeNull();
        retrievedSession!.Id.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetSessionAsync_WithNonExistentSession_ShouldReturnNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var session = await _sessionManager.GetSessionAsync(sessionId);

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public async Task IsSessionActive_WithActiveSession_ShouldReturnTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new SessionStartRequest { SessionId = sessionId };
        await _sessionManager.CreateSessionAsync(request);

        // Act
        var isActive = _sessionManager.IsSessionActive(sessionId);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public async Task IsSessionActive_AfterEndingSession_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new SessionStartRequest { SessionId = sessionId };
        await _sessionManager.CreateSessionAsync(request);
        await _sessionManager.EndSessionAsync(sessionId);

        // Act
        var isActive = _sessionManager.IsSessionActive(sessionId);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateQualitySettingsAsync_WithExistingSession_ShouldUpdateQuality()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new SessionStartRequest 
        { 
            SessionId = sessionId,
            InitialQuality = 50
        };
        await _sessionManager.CreateSessionAsync(request);

        // Act
        await _sessionManager.UpdateQualitySettingsAsync(sessionId, 95);

        // Assert
        var session = await _sessionManager.GetSessionAsync(sessionId);
        session!.QualitySettings.Quality.Should().Be(95);
        _loggerMock.VerifyLog(LogLevel.Information, "Updated quality settings", Times.Once());
    }

    [Fact]
    public async Task UpdateQualitySettingsAsync_WithNonExistentSession_ShouldNotThrow()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act & Assert
        await _sessionManager.UpdateQualitySettingsAsync(sessionId, 95);
        // Should complete without throwing
    }

    [Fact]
    public async Task SessionInfo_IsTimedOut_ShouldReturnTrueAfter8Hours()
    {
        // Arrange
        var session = new SessionInfo
        {
            StartTime = DateTime.UtcNow.AddHours(-9),
            Status = SessionStatus.Active
        };

        // Act & Assert
        session.IsTimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task SessionInfo_IsTimedOut_ShouldReturnFalseWithin8Hours()
    {
        // Arrange
        var session = new SessionInfo
        {
            StartTime = DateTime.UtcNow.AddHours(-7),
            Status = SessionStatus.Active
        };

        // Act & Assert
        session.IsTimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task SessionInfo_Duration_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;
        var session = new SessionInfo
        {
            StartTime = startTime,
            EndTime = endTime
        };

        // Act
        var duration = session.Duration;

        // Assert
        duration.Should().BeCloseTo(TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(1));
    }
}