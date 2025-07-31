using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class AuditServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<AuditService>> _loggerMock;
        private readonly Mock<IBackgroundTaskQueue> _taskQueueMock;
        private readonly IServiceProvider _serviceProvider;
        private readonly AuditService _auditService;
        private readonly AuditOptions _options;

        public AuditServiceTests()
        {
            // Setup in-memory database
            var dbOptions = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(dbOptions);

            // Setup mocks
            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<AuditService>>();
            _taskQueueMock = new Mock<IBackgroundTaskQueue>();

            // Setup service provider
            var services = new ServiceCollection();
            services.AddScoped(_ => _context);
            _serviceProvider = services.BuildServiceProvider();

            // Setup options
            _options = new AuditOptions
            {
                EnableBatching = false,
                MinimumSeverity = AuditSeverity.Info,
                RetentionDays = 365
            };

            // Create service
            _auditService = new AuditService(
                _serviceProvider,
                _cacheMock.Object,
                _loggerMock.Object,
                Options.Create(_options),
                _taskQueueMock.Object);
        }

        [Fact]
        public async Task LogAsync_ValidEntry_SavesSuccessfully()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            // Add organization and user
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            _context.Users.Add(new User { Id = userId, Email = "test@example.com" });
            await _context.SaveChangesAsync();

            var entry = new AuditLogEntry
            {
                OrganizationId = organizationId,
                UserId = userId,
                Action = "TestAction",
                ResourceType = "TestResource",
                ResourceId = "123",
                Severity = AuditSeverity.Info,
                Category = AuditCategory.General,
                Details = "Test audit entry"
            };

            // Act
            await _auditService.LogAsync(entry);

            // Assert
            var savedEntry = await _context.AuditLogs.FirstOrDefaultAsync();
            Assert.NotNull(savedEntry);
            Assert.Equal("TestAction", savedEntry.Action);
            Assert.Equal("TestResource", savedEntry.ResourceType);
            Assert.Equal("123", savedEntry.ResourceId);
            Assert.Equal("Test audit entry", savedEntry.Details);
        }

        [Fact]
        public async Task LogAsync_WithMinimumSeverity_FiltersCorrectly()
        {
            // Arrange
            _options.MinimumSeverity = AuditSeverity.Warning;
            var organizationId = Guid.NewGuid();
            
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            await _context.SaveChangesAsync();

            var infoEntry = new AuditLogEntry
            {
                OrganizationId = organizationId,
                Action = "InfoAction",
                ResourceType = "Resource",
                Severity = AuditSeverity.Info
            };

            var warningEntry = new AuditLogEntry
            {
                OrganizationId = organizationId,
                Action = "WarningAction",
                ResourceType = "Resource",
                Severity = AuditSeverity.Warning
            };

            // Act
            await _auditService.LogAsync(infoEntry);
            await _auditService.LogAsync(warningEntry);

            // Assert
            var entries = await _context.AuditLogs.ToListAsync();
            Assert.Single(entries);
            Assert.Equal("WarningAction", entries[0].Action);
        }

        [Fact]
        public async Task LogAsync_WithExcludedAction_IsIgnored()
        {
            // Arrange
            _options.ExcludedActions = new List<string> { "IgnoredAction" };
            var organizationId = Guid.NewGuid();
            
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            await _context.SaveChangesAsync();

            var entry = new AuditLogEntry
            {
                OrganizationId = organizationId,
                Action = "IgnoredAction",
                ResourceType = "Resource",
                Severity = AuditSeverity.Info
            };

            // Act
            await _auditService.LogAsync(entry);

            // Assert
            var entries = await _context.AuditLogs.ToListAsync();
            Assert.Empty(entries);
        }

        [Fact]
        public async Task LogBatchAsync_MultiplEntries_SavesAll()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            await _context.SaveChangesAsync();

            var entries = new List<AuditLogEntry>
            {
                new() { OrganizationId = organizationId, Action = "Action1", ResourceType = "Resource1" },
                new() { OrganizationId = organizationId, Action = "Action2", ResourceType = "Resource2" },
                new() { OrganizationId = organizationId, Action = "Action3", ResourceType = "Resource3" }
            };

            // Act
            await _auditService.LogBatchAsync(entries);

            // Assert
            var savedEntries = await _context.AuditLogs.ToListAsync();
            Assert.Equal(3, savedEntries.Count);
            Assert.Contains(savedEntries, e => e.Action == "Action1");
            Assert.Contains(savedEntries, e => e.Action == "Action2");
            Assert.Contains(savedEntries, e => e.Action == "Action3");
        }

        [Fact]
        public async Task QueryAsync_WithFilters_ReturnsFilteredResults()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            _context.Users.Add(new User { Id = userId, Email = "test@example.com" });
            
            // Add test data
            for (int i = 0; i < 20; i++)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = i < 10 ? userId : null,
                    Action = i < 5 ? "Action1" : "Action2",
                    ResourceType = "Resource",
                    Timestamp = DateTime.UtcNow.AddHours(-i),
                    Severity = (int)(i < 3 ? RemoteC.Shared.Models.AuditSeverity.Error : RemoteC.Shared.Models.AuditSeverity.Info),
                    Category = (int)RemoteC.Shared.Models.AuditCategory.General,
                    Success = true
                });
            }
            await _context.SaveChangesAsync();

            var query = new AuditLogQuery
            {
                OrganizationId = organizationId,
                UserId = userId,
                Action = "Action1",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _auditService.QueryAsync(query);

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(5, result.Items.Count);
            Assert.All(result.Items, item =>
            {
                Assert.Equal("Action1", item.Action);
                Assert.Equal(userId, item.UserId);
            });
        }

        [Fact]
        public async Task GetByResourceAsync_WithCaching_UsesCachedResults()
        {
            // Arrange
            var resourceType = "TestResource";
            var resourceId = "123";
            var cachedData = "[{\"Action\":\"CachedAction\"}]";
            
            _cacheMock.Setup(c => c.GetAsync(
                It.Is<string>(key => key.Contains(resourceType) && key.Contains(resourceId)),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(cachedData));

            // Act
            var result = await _auditService.GetByResourceAsync(resourceType, resourceId);

            // Assert
            Assert.Single(result);
            Assert.Equal("CachedAction", result[0].Action);
            _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteOldLogsAsync_RemovesOldEntries()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            
            // Add old entries
            for (int i = 0; i < 5; i++)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Action = "OldAction",
                    ResourceType = "Resource",
                    Timestamp = cutoffDate.AddDays(-i - 1),
                    Severity = (int)RemoteC.Shared.Models.AuditSeverity.Info,
                    Category = (int)RemoteC.Shared.Models.AuditCategory.General,
                    Success = true
                });
            }
            
            // Add recent entries
            for (int i = 0; i < 3; i++)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Action = "RecentAction",
                    ResourceType = "Resource",
                    Timestamp = DateTime.UtcNow.AddDays(-i),
                    Severity = (int)RemoteC.Shared.Models.AuditSeverity.Info,
                    Category = (int)RemoteC.Shared.Models.AuditCategory.General,
                    Success = true
                });
            }
            
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _auditService.DeleteOldLogsAsync(cutoffDate);

            // Assert
            Assert.Equal(5, deletedCount);
            var remainingEntries = await _context.AuditLogs.ToListAsync();
            Assert.Equal(3, remainingEntries.Count);
            Assert.All(remainingEntries, entry => Assert.Equal("RecentAction", entry.Action));
        }

        [Fact]
        public async Task GetStatisticsAsync_ReturnsCorrectStats()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();
            
            _context.Organizations.Add(new Organization { Id = organizationId, Name = "Test Org" });
            _context.Users.Add(new User { Id = userId1, Email = "user1@example.com", FirstName = "User", LastName = "One" });
            _context.Users.Add(new User { Id = userId2, Email = "user2@example.com", FirstName = "User", LastName = "Two" });
            
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(1);
            
            // Add test data
            _context.AuditLogs.AddRange(
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = userId1,
                    UserName = "User One",
                    Action = "Login",
                    ResourceType = "Session",
                    Timestamp = startDate.AddHours(1),
                    Severity = (int)RemoteC.Shared.Models.AuditSeverity.Info,
                    Category = (int)RemoteC.Shared.Models.AuditCategory.Authentication,
                    Success = true,
                    Duration = TimeSpan.FromMilliseconds(100)
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = userId1,
                    UserName = "User One",
                    Action = "Login",
                    ResourceType = "Session",
                    Timestamp = startDate.AddHours(2),
                    Severity = (int)RemoteC.Shared.Models.AuditSeverity.Info,
                    Category = (int)RemoteC.Shared.Models.AuditCategory.Authentication,
                    Success = true,
                    Duration = TimeSpan.FromMilliseconds(150)
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = userId2,
                    UserName = "User Two",
                    Action = "UpdateProfile",
                    ResourceType = "User",
                    Timestamp = startDate.AddHours(3),
                    Severity = (int)RemoteC.Shared.Models.AuditSeverity.Info,
                    Category = (int)RemoteC.Shared.Models.AuditCategory.DataModification,
                    Success = false,
                    Duration = TimeSpan.FromMilliseconds(200)
                }
            );
            
            await _context.SaveChangesAsync();

            // Act
            var stats = await _auditService.GetStatisticsAsync(organizationId, startDate, endDate);

            // Assert
            Assert.Equal(3, stats.TotalEvents);
            Assert.Equal(1, stats.FailedEvents);
            Assert.Equal(150, stats.AverageResponseTime); // (100 + 150 + 200) / 3
            Assert.Equal(2, stats.EventsByAction["Login"]);
            Assert.Equal(1, stats.EventsByAction["UpdateProfile"]);
            Assert.Contains("User1", stats.TopUsers);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _auditService?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}