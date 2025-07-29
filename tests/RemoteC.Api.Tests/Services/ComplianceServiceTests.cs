using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    public class ComplianceServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<ILogger<ComplianceService>> _loggerMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly Mock<IEncryptionService> _encryptionMock;
        private readonly ComplianceService _service;
        private readonly ComplianceOptions _options;

        public ComplianceServiceTests()
        {
            // Setup in-memory database
            var dbOptions = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(dbOptions);

            // Setup mocks
            _loggerMock = new Mock<ILogger<ComplianceService>>();
            _auditMock = new Mock<IAuditService>();
            _encryptionMock = new Mock<IEncryptionService>();

            // Setup options
            _options = new ComplianceOptions
            {
                EnableSOC2 = true,
                EnableGDPR = true,
                EnableHIPAA = true,
                DataRetentionDays = 365,
                RequireDataEncryption = true,
                RequireAuditLogging = true
            };

            // Create service
            _service = new ComplianceService(
                _context,
                _loggerMock.Object,
                _auditMock.Object,
                _encryptionMock.Object,
                Options.Create(_options));
        }

        #region SOC2 Compliance Tests

        [Fact]
        public async Task ValidateSOC2ComplianceAsync_AllControlsMet_ReturnsCompliant()
        {
            // Arrange
            var organizationId = await CreateTestOrganization(
                enableMfa: true,
                enableEncryption: true,
                enableAuditLogging: true);

            // Act
            var result = await _service.ValidateSOC2ComplianceAsync(organizationId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsCompliant);
            Assert.Empty(result.Violations);
            Assert.All(result.Controls, control => Assert.Equal(ControlStatus.Satisfied, control.Status));
        }

        [Fact]
        public async Task ValidateSOC2ComplianceAsync_MissingMFA_ReturnsViolation()
        {
            // Arrange
            var organizationId = await CreateTestOrganization(enableMfa: false);

            // Act
            var result = await _service.ValidateSOC2ComplianceAsync(organizationId);

            // Assert
            Assert.False(result.IsCompliant);
            Assert.Contains(result.Violations, v => v.ControlId == "SOC2-ACC-001");
            Assert.Contains(result.Violations, v => v.Severity == ViolationSeverity.High);
        }

        [Fact]
        public async Task GetSOC2ControlsAsync_ReturnsAllRequiredControls()
        {
            // Act
            var controls = await _service.GetSOC2ControlsAsync();

            // Assert
            Assert.NotEmpty(controls);
            Assert.Contains(controls, c => c.Category == "Security");
            Assert.Contains(controls, c => c.Category == "Availability");
            Assert.Contains(controls, c => c.Category == "Processing Integrity");
            Assert.Contains(controls, c => c.Category == "Confidentiality");
            Assert.Contains(controls, c => c.Category == "Privacy");
        }

        [Fact]
        public async Task GenerateSOC2ReportAsync_CreatesComprehensiveReport()
        {
            // Arrange
            var organizationId = await CreateTestOrganization();
            var startDate = DateTime.UtcNow.AddMonths(-3);
            var endDate = DateTime.UtcNow;

            // Act
            var report = await _service.GenerateSOC2ReportAsync(
                organizationId, 
                startDate, 
                endDate);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(organizationId, report.OrganizationId);
            Assert.Equal(startDate, report.PeriodStart);
            Assert.Equal(endDate, report.PeriodEnd);
            Assert.NotEmpty(report.ControlAssessments);
            Assert.NotNull(report.ExecutiveSummary);
            Assert.NotNull(report.AuditorStatement);
        }

        #endregion

        #region GDPR Compliance Tests

        [Fact]
        public async Task ValidateGDPRComplianceAsync_AllRequirementsMet_ReturnsCompliant()
        {
            // Arrange
            var organizationId = await CreateTestOrganization();
            await CreatePrivacyPolicy(organizationId);
            await CreateDataProcessingAgreements(organizationId);

            // Act
            var result = await _service.ValidateGDPRComplianceAsync(organizationId);

            // Assert
            Assert.True(result.IsCompliant);
            Assert.Empty(result.Violations);
            Assert.True(result.HasPrivacyPolicy);
            Assert.True(result.HasDataProcessingAgreements);
            Assert.True(result.HasConsentMechanism);
        }

        [Fact]
        public async Task ProcessDataSubjectRequestAsync_RightToAccess_ReturnsUserData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = await CreateTestUser(userId, "test@example.com");
            await CreateUserActivities(userId);

            var request = new DataSubjectRequest
            {
                UserId = userId,
                RequestType = DataSubjectRequestType.Access,
                RequestedBy = "test@example.com"
            };

            // Act
            var response = await _service.ProcessDataSubjectRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(DataSubjectRequestStatus.Completed, response.Status);
            Assert.NotNull(response.Data);
            Assert.Contains("PersonalData", response.Data.Keys);
            Assert.Contains("ActivityLogs", response.Data.Keys);
            Assert.Contains("Sessions", response.Data.Keys);
        }

        [Fact]
        public async Task ProcessDataSubjectRequestAsync_RightToErasure_AnonymizesData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = await CreateTestUser(userId, "delete@example.com");

            var request = new DataSubjectRequest
            {
                UserId = userId,
                RequestType = DataSubjectRequestType.Erasure,
                RequestedBy = "delete@example.com"
            };

            // Act
            var response = await _service.ProcessDataSubjectRequestAsync(request);

            // Assert
            Assert.Equal(DataSubjectRequestStatus.Completed, response.Status);
            
            // Verify user data is anonymized
            var anonymizedUser = await _context.Users.FindAsync(userId);
            Assert.NotNull(anonymizedUser);
            Assert.StartsWith("ANONYMIZED_", anonymizedUser.Email);
            Assert.Equal("Anonymized User", anonymizedUser.Name);
            Assert.Null(anonymizedUser.PhoneNumber);
        }

        [Fact]
        public async Task ProcessDataSubjectRequestAsync_RightToPortability_ExportsData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            await CreateTestUser(userId, "export@example.com");

            var request = new DataSubjectRequest
            {
                UserId = userId,
                RequestType = DataSubjectRequestType.Portability,
                RequestedBy = "export@example.com",
                ExportFormat = ExportFormat.Json
            };

            // Act
            var response = await _service.ProcessDataSubjectRequestAsync(request);

            // Assert
            Assert.Equal(DataSubjectRequestStatus.Completed, response.Status);
            Assert.NotNull(response.ExportedData);
            Assert.True(response.ExportedData.Length > 0);
            
            // Verify it's valid JSON
            var json = System.Text.Encoding.UTF8.GetString(response.ExportedData);
            var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.NotNull(parsed);
        }

        [Fact]
        public async Task GetConsentRecordsAsync_ReturnsUserConsents()
        {
            // Arrange
            var userId = Guid.NewGuid();
            await CreateConsentRecords(userId);

            // Act
            var consents = await _service.GetConsentRecordsAsync(userId);

            // Assert
            Assert.NotEmpty(consents);
            Assert.All(consents, c => Assert.Equal(userId, c.UserId));
            Assert.Contains(consents, c => c.Purpose == "Marketing");
            Assert.Contains(consents, c => c.Purpose == "Analytics");
        }

        [Fact]
        public async Task UpdateConsentAsync_RecordsConsentChange()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var consentUpdate = new ConsentUpdate
            {
                UserId = userId,
                Purpose = "Marketing",
                Granted = false,
                UpdatedBy = "user@example.com"
            };

            // Act
            var result = await _service.UpdateConsentAsync(consentUpdate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("Marketing", result.Purpose);
            Assert.False(result.Granted);
            Assert.Equal("user@example.com", result.UpdatedBy);
            
            // Verify audit log
            _auditMock.Verify(a => a.LogAsync(
                It.Is<AuditLogEntry>(e => 
                    e.Action == "ConsentUpdate" && 
                    e.Category == AuditCategory.Compliance),
                default), 
                Times.Once);
        }

        #endregion

        #region HIPAA Compliance Tests

        [Fact]
        public async Task ValidateHIPAAComplianceAsync_AllSafeguardsMet_ReturnsCompliant()
        {
            // Arrange
            var organizationId = await CreateTestOrganization(
                enableEncryption: true,
                enableAuditLogging: true,
                enableAccessControls: true);

            // Act
            var result = await _service.ValidateHIPAAComplianceAsync(organizationId);

            // Assert
            Assert.True(result.IsCompliant);
            Assert.Empty(result.Violations);
            Assert.True(result.HasEncryptionAtRest);
            Assert.True(result.HasEncryptionInTransit);
            Assert.True(result.HasAccessControls);
            Assert.True(result.HasAuditControls);
        }

        [Fact]
        public async Task CheckPHIAccessAsync_ValidatesMinimumNecessaryRule()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var resourceId = Guid.NewGuid();
            await GrantPHIAccess(userId, resourceId, "Treatment");

            // Act
            var hasAccess = await _service.CheckPHIAccessAsync(userId, resourceId, "Treatment");
            var noAccess = await _service.CheckPHIAccessAsync(userId, resourceId, "Marketing");

            // Assert
            Assert.True(hasAccess);
            Assert.False(noAccess); // Minimum necessary rule
        }

        [Fact]
        public async Task GenerateHIPAABreachReportAsync_IncludesRequiredElements()
        {
            // Arrange
            var breach = new SecurityBreach
            {
                BreachDate = DateTime.UtcNow.AddDays(-2),
                DiscoveryDate = DateTime.UtcNow.AddDays(-1),
                AffectedRecords = 150,
                BreachType = "Unauthorized Access",
                Description = "Unauthorized access to PHI records"
            };

            // Act
            var report = await _service.GenerateHIPAABreachReportAsync(breach);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.RequiresNotification); // >500 records
            Assert.NotEmpty(report.AffectedIndividuals);
            Assert.NotNull(report.RiskAssessment);
            Assert.NotEmpty(report.MitigationSteps);
            Assert.Contains("HHS", report.NotificationRequirements);
        }

        #endregion

        #region Data Retention Tests

        [Fact]
        public async Task ApplyDataRetentionPolicyAsync_DeletesExpiredData()
        {
            // Arrange
            var organizationId = await CreateTestOrganization();
            await CreateExpiredData(organizationId);
            
            // Act
            var result = await _service.ApplyDataRetentionPolicyAsync(organizationId);

            // Assert
            Assert.True(result.RecordsDeleted > 0);
            Assert.True(result.SessionsDeleted > 0);
            Assert.True(result.LogsDeleted > 0);
            
            // Verify data is actually deleted
            var remainingSessions = await _context.Sessions
                .Where(s => s.EndedAt < DateTime.UtcNow.AddDays(-_options.DataRetentionDays))
                .CountAsync();
            Assert.Equal(0, remainingSessions);
        }

        [Fact]
        public async Task GetDataRetentionStatusAsync_ReturnsAccurateMetrics()
        {
            // Arrange
            var organizationId = await CreateTestOrganization();
            await CreateMixedAgeData(organizationId);

            // Act
            var status = await _service.GetDataRetentionStatusAsync(organizationId);

            // Assert
            Assert.NotNull(status);
            Assert.True(status.TotalRecords > 0);
            Assert.True(status.RecordsToDelete > 0);
            Assert.True(status.RecordsToRetain > 0);
            Assert.Equal(status.TotalRecords, status.RecordsToDelete + status.RecordsToRetain);
        }

        #endregion

        #region Compliance Monitoring Tests

        [Fact]
        public async Task MonitorComplianceAsync_DetectsViolations()
        {
            // Arrange
            var organizationId = await CreateTestOrganization();
            await CreateComplianceViolations(organizationId);

            // Act
            var violations = await _service.MonitorComplianceAsync(organizationId);

            // Assert
            Assert.NotEmpty(violations);
            Assert.Contains(violations, v => v.Type == "UnencryptedDataTransfer");
            Assert.Contains(violations, v => v.Type == "ExcessiveDataRetention");
            Assert.Contains(violations, v => v.Type == "MissingAuditLogs");
        }

        [Fact]
        public async Task GenerateComplianceDashboardAsync_ProvidesOverview()
        {
            // Arrange
            var organizationId = await CreateTestOrganization();

            // Act
            var dashboard = await _service.GenerateComplianceDashboardAsync(organizationId);

            // Assert
            Assert.NotNull(dashboard);
            Assert.NotNull(dashboard.SOC2Status);
            Assert.NotNull(dashboard.GDPRStatus);
            Assert.NotNull(dashboard.HIPAAStatus);
            Assert.NotEmpty(dashboard.RecentAudits);
            Assert.NotEmpty(dashboard.UpcomingTasks);
            Assert.True(dashboard.OverallScore >= 0 && dashboard.OverallScore <= 100);
        }

        #endregion

        #region Helper Methods

        private async Task<Guid> CreateTestOrganization(
            bool enableMfa = true,
            bool enableEncryption = true,
            bool enableAuditLogging = true,
            bool enableAccessControls = true)
        {
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Organization",
                CreatedAt = DateTime.UtcNow
            };

            var settings = new OrganizationSettings
            {
                OrganizationId = org.Id,
                RequireMFA = enableMfa,
                EncryptionEnabled = enableEncryption,
                AuditLoggingEnabled = enableAuditLogging,
                AccessControlsEnabled = enableAccessControls
            };

            _context.Organizations.Add(org);
            _context.OrganizationSettings.Add(settings);
            await _context.SaveChangesAsync();

            return org.Id;
        }

        private async Task<User> CreateTestUser(Guid userId, string email)
        {
            var user = new User
            {
                Id = userId,
                Email = email,
                Name = "Test User",
                PhoneNumber = "+1234567890",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task CreateUserActivities(Guid userId)
        {
            var activities = new[]
            {
                new UserActivity { UserId = userId, Action = "Login", Timestamp = DateTime.UtcNow.AddDays(-1) },
                new UserActivity { UserId = userId, Action = "ViewDocument", Timestamp = DateTime.UtcNow.AddHours(-2) },
                new UserActivity { UserId = userId, Action = "UpdateProfile", Timestamp = DateTime.UtcNow.AddMinutes(-30) }
            };

            _context.UserActivities.AddRange(activities);
            await _context.SaveChangesAsync();
        }

        private async Task CreatePrivacyPolicy(Guid organizationId)
        {
            var policy = new PrivacyPolicy
            {
                OrganizationId = organizationId,
                Version = "1.0",
                EffectiveDate = DateTime.UtcNow.AddMonths(-1),
                Content = "Privacy policy content...",
                IsActive = true
            };

            _context.PrivacyPolicies.Add(policy);
            await _context.SaveChangesAsync();
        }

        private async Task CreateDataProcessingAgreements(Guid organizationId)
        {
            var dpa = new DataProcessingAgreement
            {
                OrganizationId = organizationId,
                ProcessorName = "Cloud Provider",
                SignedDate = DateTime.UtcNow.AddMonths(-2),
                IsActive = true
            };

            _context.DataProcessingAgreements.Add(dpa);
            await _context.SaveChangesAsync();
        }

        private async Task CreateConsentRecords(Guid userId)
        {
            var consents = new[]
            {
                new ConsentRecord 
                { 
                    UserId = userId, 
                    Purpose = "Marketing", 
                    Granted = true, 
                    GrantedAt = DateTime.UtcNow.AddDays(-30) 
                },
                new ConsentRecord 
                { 
                    UserId = userId, 
                    Purpose = "Analytics", 
                    Granted = false, 
                    GrantedAt = DateTime.UtcNow.AddDays(-15) 
                }
            };

            _context.ConsentRecords.AddRange(consents);
            await _context.SaveChangesAsync();
        }

        private async Task GrantPHIAccess(Guid userId, Guid resourceId, string purpose)
        {
            var access = new PHIAccess
            {
                UserId = userId,
                ResourceId = resourceId,
                Purpose = purpose,
                GrantedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _context.PHIAccesses.Add(access);
            await _context.SaveChangesAsync();
        }

        private async Task CreateExpiredData(Guid organizationId)
        {
            var oldDate = DateTime.UtcNow.AddDays(-400); // Older than retention period

            var sessions = Enumerable.Range(0, 10).Select(i => new Session
            {
                Id = Guid.NewGuid(),
                CreatedAt = oldDate,
                EndedAt = oldDate.AddHours(1)
            });

            _context.Sessions.AddRange(sessions);
            await _context.SaveChangesAsync();
        }

        private async Task CreateMixedAgeData(Guid organizationId)
        {
            // Create some old data
            await CreateExpiredData(organizationId);

            // Create some recent data
            var recentSessions = Enumerable.Range(0, 5).Select(i => new Session
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                EndedAt = DateTime.UtcNow.AddDays(-9)
            });

            _context.Sessions.AddRange(recentSessions);
            await _context.SaveChangesAsync();
        }

        private async Task CreateComplianceViolations(Guid organizationId)
        {
            // Create various violations for monitoring
            // This would be detected by the monitoring logic
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #endregion
    }

    // Test models for compliance features
    public enum DataSubjectRequestType
    {
        Access,
        Rectification,
        Erasure,
        Portability,
        Restriction,
        Objection
    }

    public enum DataSubjectRequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Rejected
    }

    public enum ViolationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ControlStatus
    {
        Satisfied,
        PartiallyMet,
        NotMet,
        NotApplicable
    }

    public enum ExportFormat
    {
        Json,
        Csv,
        Xml
    }

    public class ComplianceOptions
    {
        public bool EnableSOC2 { get; set; }
        public bool EnableGDPR { get; set; }
        public bool EnableHIPAA { get; set; }
        public int DataRetentionDays { get; set; }
        public bool RequireDataEncryption { get; set; }
        public bool RequireAuditLogging { get; set; }
    }

    // Additional test entities
    public class OrganizationSettings
    {
        public Guid OrganizationId { get; set; }
        public bool RequireMFA { get; set; }
        public bool EncryptionEnabled { get; set; }
        public bool AuditLoggingEnabled { get; set; }
        public bool AccessControlsEnabled { get; set; }
    }

    public class UserActivity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class PrivacyPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class DataProcessingAgreement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public string ProcessorName { get; set; } = string.Empty;
        public DateTime SignedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ConsentRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public bool Granted { get; set; }
        public DateTime GrantedAt { get; set; }
    }

    public class PHIAccess
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid ResourceId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}