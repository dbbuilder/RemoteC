using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using RemoteC.Api.Services;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using RemoteC.Api.Hubs;

namespace RemoteC.Api.Tests.Services
{
    /// <summary>
    /// TDD test suite for PIN-based session joining functionality
    /// </summary>
    public class PinSessionServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<IPinService> _mockPinService;
        private readonly Mock<IHubContext<SessionHub>> _mockHubContext;
        private readonly Mock<ILogger<SessionService>> _mockLogger;
        private readonly SessionService _sessionService;

        public PinSessionServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(options);

            // Setup mocks
            _mockPinService = new Mock<IPinService>();
            _mockHubContext = new Mock<IHubContext<SessionHub>>();
            _mockLogger = new Mock<ILogger<SessionService>>();

            // Setup SignalR mocks
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            _mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
            mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

            var mockRemoteControlService = new Mock<IRemoteControlService>();
            var mockAuditService = new Mock<IAuditService>();
            var mockMapper = new Mock<AutoMapper.IMapper>();

            _sessionService = new SessionService(
                _context,
                mockMapper.Object,
                _mockPinService.Object,
                mockRemoteControlService.Object,
                mockAuditService.Object,
                _mockLogger.Object,
                _mockHubContext.Object
            );

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var device = new Device
            {
                Id = Guid.NewGuid(),
                Name = "Test Host",
                HostName = "TEST-PC",
                OperatingSystem = "Windows 10",
                RegisteredBy = Guid.NewGuid(),
                LastSeenAt = DateTime.UtcNow
            };

            var session = new Session
            {
                Id = Guid.NewGuid(),
                Name = "Test Session",
                DeviceId = device.Id,
                Device = device,
                CreatedBy = Guid.NewGuid(),
                RequirePin = true,
                Status = Data.Entities.SessionStatus.WaitingForPin,
                CreatedAt = DateTime.UtcNow
            };

            _context.Devices.Add(device);
            _context.Sessions.Add(session);
            _context.SaveChanges();
        }

        [Fact]
        public async Task JoinSessionWithPin_ValidPin_ShouldSucceed()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(true);

            // Act
            var result = await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(session.Id, result.SessionId);
            Assert.NotNull(result.ConnectionToken);
            Assert.Equal("Guest", result.UserRole);

            // Verify PIN was validated
            _mockPinService.Verify(x => x.ValidatePinAsync(session.Id, pin), Times.Once);

            // Verify participant was added
            var participant = await _context.SessionParticipants
                .FirstOrDefaultAsync(p => p.SessionId == session.Id && p.UserId.ToString() == userId);
            Assert.NotNull(participant);
            Assert.Equal(Data.Entities.ParticipantRole.Viewer, participant.Role);
        }

        [Fact]
        public async Task JoinSessionWithPin_InvalidPin_ShouldFail()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "999999";
            var userId = Guid.NewGuid().ToString();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId));

            // Verify no participant was added
            var participant = await _context.SessionParticipants
                .FirstOrDefaultAsync(p => p.SessionId == session.Id && p.UserId.ToString() == userId);
            Assert.Null(participant);
        }

        [Fact]
        public async Task JoinSessionWithPin_ExpiredPin_ShouldFail()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(false); // PIN validation fails (expired)

            _mockPinService.Setup(x => x.GetPinDetailsAsync(pin))
                .ReturnsAsync(new PinDetails
                {
                    SessionId = session.Id,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired 5 minutes ago
                    IsUsed = false
                });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId));
        }

        [Fact]
        public async Task JoinSessionWithPin_AlreadyUsedPin_ShouldFail()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(false); // PIN validation fails (already used)

            _mockPinService.Setup(x => x.GetPinDetailsAsync(pin))
                .ReturnsAsync(new PinDetails
                {
                    SessionId = session.Id,
                    IsUsed = true,
                    UsedAt = DateTime.UtcNow.AddMinutes(-2)
                });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId));
        }

        [Fact]
        public async Task JoinSessionWithPin_SessionNotRequiringPin_ShouldFail()
        {
            // Arrange
            var device = await _context.Devices.FirstAsync();
            var sessionWithoutPin = new Session
            {
                Id = Guid.NewGuid(),
                Name = "No PIN Session",
                DeviceId = device.Id,
                Device = device,
                CreatedBy = Guid.NewGuid(),
                RequirePin = false, // No PIN required
                Status = Data.Entities.SessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Sessions.Add(sessionWithoutPin);
            await _context.SaveChangesAsync();

            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _sessionService.JoinSessionWithPinAsync(sessionWithoutPin.Id, pin, userId));
        }

        [Fact]
        public async Task JoinSessionWithPin_DuplicateJoin_ShouldSucceed()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            // Add existing participant
            var existingParticipant = new SessionParticipant
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserId = Guid.Parse(userId),
                Role = Data.Entities.ParticipantRole.Viewer,
                JoinedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            _context.SessionParticipants.Add(existingParticipant);
            await _context.SaveChangesAsync();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(true);

            // Act
            var result = await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(session.Id, result.SessionId);

            // Verify only one participant exists
            var participantCount = await _context.SessionParticipants
                .CountAsync(p => p.SessionId == session.Id && p.UserId.ToString() == userId);
            Assert.Equal(1, participantCount);
        }

        [Fact]
        public async Task JoinSessionWithPin_ShouldUpdateSessionStatus()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(true);

            // Act
            await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId);

            // Assert
            var updatedSession = await _context.Sessions.FindAsync(session.Id);
            Assert.Equal(Data.Entities.SessionStatus.Active, updatedSession.Status);
            Assert.NotNull(updatedSession.StartedAt);
        }

        [Fact]
        public async Task JoinSessionWithPin_ShouldNotifyViaSignalR()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";
            var userId = Guid.NewGuid().ToString();

            _mockPinService.Setup(x => x.ValidatePinAsync(session.Id, pin))
                .ReturnsAsync(true);

            // Act
            await _sessionService.JoinSessionWithPinAsync(session.Id, pin, userId);

            // Assert
            _mockHubContext.Verify(x => x.Clients.Group(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GenerateTemporaryPin_ShouldCreateShortLivedPin()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var userId = session.CreatedBy.ToString();
            var expirationMinutes = 5;

            var expectedPin = "123456";
            _mockPinService.Setup(x => x.GeneratePinWithDetailsAsync(session.Id, expirationMinutes))
                .ReturnsAsync(new ExtendedPinGenerationResult
                {
                    PinCode = expectedPin,
                    SessionId = session.Id,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
                });

            // Act
            var result = await _sessionService.GenerateTemporaryPinAsync(session.Id, userId, expirationMinutes);

            // Assert
            Assert.Equal(expectedPin, result.PinCode);
            Assert.Equal(session.Id, result.SessionId);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);
            Assert.True(result.ExpiresAt <= DateTime.UtcNow.AddMinutes(expirationMinutes + 1));
        }

        [Fact]
        public async Task ValidatePinBeforeJoin_ShouldCheckPinValidity()
        {
            // Arrange
            var session = await _context.Sessions.FirstAsync();
            var pin = "123456";

            _mockPinService.Setup(x => x.IsPinValidAsync(session.Id, pin))
                .ReturnsAsync(true);

            // Act
            var isValid = await _sessionService.ValidatePinBeforeJoinAsync(session.Id, pin);

            // Assert
            Assert.True(isValid);
            _mockPinService.Verify(x => x.IsPinValidAsync(session.Id, pin), Times.Once);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}