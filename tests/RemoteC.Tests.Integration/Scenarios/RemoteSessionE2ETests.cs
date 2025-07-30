using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemoteC.Api;
using RemoteC.Data;
using RemoteC.Tests.Integration.TestFixtures;
using Xunit;
using FluentAssertions;

namespace RemoteC.Tests.Integration.Scenarios
{
    /// <summary>
    /// End-to-end tests for complete remote session scenarios
    /// </summary>
    [Collection("Database")]
    public class RemoteSessionE2ETests : IntegrationTestBase
    {
        public RemoteSessionE2ETests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CompleteRemoteSessionLifecycle_ShouldWorkEndToEnd()
        {
            // This test simulates a complete remote session from start to finish
            await ClearTestDataAsync();

            // Step 1: Create test data
            var (user, device) = await CreateTestUserAndDeviceAsync();

            // Step 2: Verify device is registered
            using (var scope = CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
                var savedDevice = await dbContext.Devices.FindAsync(device.Id);
                
                savedDevice.Should().NotBeNull();
                savedDevice!.IsOnline.Should().BeTrue();
            }

            // Step 3: Create a session (would normally be done via API)
            var session = await CreateSessionAsync(user.Id, device.Id);

            // Step 4: Verify session is created
            using (var scope = CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
                var savedSession = await dbContext.Sessions.FindAsync(session.Id);
                
                savedSession.Should().NotBeNull();
                savedSession!.Status.Should().Be(RemoteC.Data.Entities.SessionStatus.Created);
            }

            // Step 5: Start the session
            await UpdateSessionStatusAsync(session.Id, RemoteC.Data.Entities.SessionStatus.Active);

            // Step 6: Create audit logs for session activities
            await CreateAuditLogsAsync(session.Id, user.Id);

            // Step 7: End the session
            await UpdateSessionStatusAsync(session.Id, RemoteC.Data.Entities.SessionStatus.Ended);

            // Step 8: Verify complete session history
            using (var scope = CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
                
                // Check session final status
                var finalSession = await dbContext.Sessions.FindAsync(session.Id);
                finalSession!.Status.Should().Be(RemoteC.Data.Entities.SessionStatus.Ended);
                finalSession.EndedAt.Should().NotBeNull();
                
                // Check audit logs
                var auditLogs = dbContext.AuditLogs
                    .Where(a => a.ResourceId == session.Id.ToString())
                    .ToList();
                    
                auditLogs.Should().HaveCountGreaterThan(0);
                auditLogs.Should().Contain(a => a.Action == "SessionStarted");
                auditLogs.Should().Contain(a => a.Action == "SessionEnded");
            }
        }

        [Fact]
        public async Task FileTransferDuringSession_ShouldTrackProperly()
        {
            // Setup
            await ClearTestDataAsync();
            var (user, device) = await CreateTestUserAndDeviceAsync();
            var session = await CreateSessionAsync(user.Id, device.Id);

            // Create a file transfer
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();
            
            var fileTransfer = new RemoteC.Data.Entities.FileTransfer
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserId = user.Id,
                FileName = "test-document.pdf",
                TotalSize = 1024 * 1024, // 1MB
                ChunkSize = 65536, // 64KB chunks
                TotalChunks = 16,
                ChunksReceived = 0,
                Direction = RemoteC.Data.Entities.TransferDirection.Upload,
                Status = RemoteC.Data.Entities.TransferStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            dbContext.FileTransfers.Add(fileTransfer);
            await dbContext.SaveChangesAsync();

            // Simulate chunk uploads
            for (int i = 0; i < fileTransfer.TotalChunks; i++)
            {
                fileTransfer.ChunksReceived = i + 1;
                fileTransfer.BytesReceived = (i + 1) * fileTransfer.ChunkSize;
                fileTransfer.UpdatedAt = DateTime.UtcNow;
                
                if (i == fileTransfer.TotalChunks - 1)
                {
                    fileTransfer.Status = RemoteC.Data.Entities.TransferStatus.Completed;
                    fileTransfer.CompletedAt = DateTime.UtcNow;
                }
                
                await dbContext.SaveChangesAsync();
            }

            // Verify transfer completed
            var completedTransfer = await dbContext.FileTransfers.FindAsync(fileTransfer.Id);
            completedTransfer.Should().NotBeNull();
            completedTransfer!.Status.Should().Be(RemoteC.Data.Entities.TransferStatus.Completed);
            completedTransfer.ChunksReceived.Should().Be(fileTransfer.TotalChunks);
            completedTransfer.CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task MultipleUsersInSession_ShouldTrackParticipants()
        {
            // Setup
            await ClearTestDataAsync();
            var (hostUser, device) = await CreateTestUserAndDeviceAsync();
            var guestUser = await CreateUserAsync("guest@example.com", "Guest", "User");
            var session = await CreateSessionAsync(hostUser.Id, device.Id);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();

            // Add session participants
            var hostParticipant = new RemoteC.Data.Entities.SessionParticipant
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserId = hostUser.Id,
                Role = RemoteC.Data.Entities.ParticipantRole.Controller,
                JoinedAt = DateTime.UtcNow,
                IsConnected = true
            };

            var guestParticipant = new RemoteC.Data.Entities.SessionParticipant
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                UserId = guestUser.Id,
                Role = RemoteC.Data.Entities.ParticipantRole.Viewer,
                JoinedAt = DateTime.UtcNow.AddSeconds(30),
                IsConnected = true
            };

            dbContext.SessionParticipants.AddRange(hostParticipant, guestParticipant);
            await dbContext.SaveChangesAsync();

            // Query participants
            var participants = dbContext.SessionParticipants
                .Where(p => p.SessionId == session.Id)
                .ToList();

            // Verify
            participants.Should().HaveCount(2);
            participants.Should().Contain(p => p.Role == RemoteC.Data.Entities.ParticipantRole.Controller);
            participants.Should().Contain(p => p.Role == RemoteC.Data.Entities.ParticipantRole.Viewer);
            participants.All(p => p.IsConnected).Should().BeTrue();
        }

        private async Task<(RemoteC.Data.Entities.User user, RemoteC.Data.Entities.Device device)> CreateTestUserAndDeviceAsync()
        {
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();

            var user = await CreateUserAsync("test@example.com", "Test", "User");

            var device = new RemoteC.Data.Entities.Device
            {
                Id = Guid.NewGuid(),
                Name = "Test Workstation",
                OperatingSystem = "Windows 11",
                LastSeenAt = DateTime.UtcNow,
                IsOnline = true,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Devices.Add(device);
            await dbContext.SaveChangesAsync();

            return (user, device);
        }

        private async Task<RemoteC.Data.Entities.User> CreateUserAsync(string email, string firstName, string lastName)
        {
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();

            var user = new RemoteC.Data.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            return user;
        }

        private async Task<RemoteC.Data.Entities.Session> CreateSessionAsync(Guid userId, Guid deviceId)
        {
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();

            // First create an organization
            var organization = new RemoteC.Data.Entities.Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Organization",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync();
            
            var session = new RemoteC.Data.Entities.Session
            {
                Id = Guid.NewGuid(),
                Name = "Test Session",
                CreatedBy = userId,
                DeviceId = deviceId,
                OrganizationId = organization.Id,
                Type = RemoteC.Data.Entities.SessionType.RemoteControl,
                Status = RemoteC.Data.Entities.SessionStatus.Created,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Sessions.Add(session);
            await dbContext.SaveChangesAsync();

            return session;
        }

        private async Task UpdateSessionStatusAsync(Guid sessionId, RemoteC.Data.Entities.SessionStatus status)
        {
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();

            var session = await dbContext.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Status = status;
                
                if (status == RemoteC.Data.Entities.SessionStatus.Active)
                {
                    session.StartedAt = DateTime.UtcNow;
                }
                else if (status == RemoteC.Data.Entities.SessionStatus.Ended)
                {
                    session.EndedAt = DateTime.UtcNow;
                }

                await dbContext.SaveChangesAsync();
            }
        }

        private async Task CreateAuditLogsAsync(Guid sessionId, Guid userId)
        {
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RemoteCDbContext>();

            var auditLogs = new[]
            {
                new RemoteC.Data.Entities.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = "SessionStarted",
                    EntityType = "Session",
                    EntityId = sessionId.ToString(),
                    ResourceId = sessionId.ToString(),
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    IpAddress = "192.168.1.100",
                    UserAgent = "RemoteC Client/1.0"
                },
                new RemoteC.Data.Entities.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = "FileTransferInitiated",
                    EntityType = "FileTransfer",
                    EntityId = Guid.NewGuid().ToString(),
                    ResourceId = sessionId.ToString(),
                    Timestamp = DateTime.UtcNow.AddMinutes(-3),
                    IpAddress = "192.168.1.100",
                    UserAgent = "RemoteC Client/1.0"
                },
                new RemoteC.Data.Entities.AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = "SessionEnded",
                    EntityType = "Session",
                    EntityId = sessionId.ToString(),
                    ResourceId = sessionId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    IpAddress = "192.168.1.100",
                    UserAgent = "RemoteC Client/1.0"
                }
            };

            dbContext.AuditLogs.AddRange(auditLogs);
            await dbContext.SaveChangesAsync();
        }
    }
}