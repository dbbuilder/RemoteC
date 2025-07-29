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
    public class AnalyticsServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<ILogger<AnalyticsService>> _loggerMock;
        private readonly Mock<IMetricsCollector> _metricsMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly AnalyticsService _service;
        private readonly AnalyticsOptions _options;

        public AnalyticsServiceTests()
        {
            // Setup in-memory database
            var dbOptions = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(dbOptions);

            // Setup mocks
            _loggerMock = new Mock<ILogger<AnalyticsService>>();
            _metricsMock = new Mock<IMetricsCollector>();
            _cacheMock = new Mock<ICacheService>();

            // Setup options
            _options = new AnalyticsOptions
            {
                EnableRealTimeAnalytics = true,
                DataRetentionDays = 90,
                AggregationIntervalMinutes = 5,
                EnablePredictiveAnalytics = true,
                AlertThresholds = new Dictionary<string, double>
                {
                    ["CpuUsage"] = 80.0,
                    ["MemoryUsage"] = 85.0,
                    ["SessionCount"] = 1000.0,
                    ["ErrorRate"] = 5.0
                }
            };

            // Create service
            _service = new AnalyticsService(
                _context,
                _loggerMock.Object,
                _metricsMock.Object,
                _cacheMock.Object,
                Options.Create(_options));
        }

        #region Session Analytics Tests

        [Fact]
        public async Task GetSessionAnalyticsAsync_ReturnsAccurateMetrics()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            
            await CreateSessionData(organizationId, 50);

            // Act
            var analytics = await _service.GetSessionAnalyticsAsync(
                organizationId, 
                startDate, 
                endDate);

            // Assert
            Assert.NotNull(analytics);
            Assert.Equal(50, analytics.TotalSessions);
            Assert.True(analytics.AverageSessionDuration > TimeSpan.Zero);
            Assert.True(analytics.UniqueUsers > 0);
            Assert.NotEmpty(analytics.SessionsByDay);
            Assert.NotEmpty(analytics.SessionsByHour);
            Assert.Equal(7, analytics.SessionsByDay.Count);
        }

        [Fact]
        public async Task GetRealTimeSessionMetricsAsync_ReturnsCurrentMetrics()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateActiveSessions(organizationId, 10);

            // Act
            var metrics = await _service.GetRealTimeSessionMetricsAsync(organizationId);

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(10, metrics.ActiveSessions);
            Assert.True(metrics.SessionsPerMinute >= 0);
            Assert.NotEmpty(metrics.ActiveUsersByLocation);
            Assert.NotEmpty(metrics.DeviceTypes);
            Assert.True(metrics.Timestamp > DateTime.UtcNow.AddSeconds(-10));
        }

        [Fact]
        public async Task GetSessionTrendsAsync_IdentifiesPatterns()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateTrendingSessionData(organizationId);

            // Act
            var trends = await _service.GetSessionTrendsAsync(organizationId, 30);

            // Assert
            Assert.NotNull(trends);
            Assert.True(trends.GrowthRate != 0);
            Assert.NotEmpty(trends.PeakHours);
            Assert.NotEmpty(trends.PeakDays);
            Assert.True(trends.PredictedNextWeekSessions > 0);
            Assert.Contains("Monday", trends.PeakDays);
        }

        #endregion

        #region Performance Analytics Tests

        [Fact]
        public async Task GetPerformanceMetricsAsync_TracksSystemPerformance()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreatePerformanceData(organizationId);

            // Act
            var metrics = await _service.GetPerformanceMetricsAsync(organizationId);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.AverageLatency > 0);
            Assert.True(metrics.P95Latency > metrics.AverageLatency);
            Assert.True(metrics.P99Latency > metrics.P95Latency);
            Assert.True(metrics.Throughput > 0);
            Assert.True(metrics.ErrorRate >= 0 && metrics.ErrorRate <= 100);
            Assert.NotEmpty(metrics.LatencyHistogram);
        }

        [Fact]
        public async Task GetResourceUtilizationAsync_MonitorsResources()
        {
            // Arrange
            var serverId = "server-001";
            _metricsMock.Setup(m => m.GetGaugeValue("cpu_usage", It.IsAny<Dictionary<string, string>>()))
                .Returns(45.5);
            _metricsMock.Setup(m => m.GetGaugeValue("memory_usage", It.IsAny<Dictionary<string, string>>()))
                .Returns(62.3);
            _metricsMock.Setup(m => m.GetGaugeValue("disk_usage", It.IsAny<Dictionary<string, string>>()))
                .Returns(78.1);

            // Act
            var utilization = await _service.GetResourceUtilizationAsync(serverId);

            // Assert
            Assert.NotNull(utilization);
            Assert.Equal(45.5, utilization.CpuUsage);
            Assert.Equal(62.3, utilization.MemoryUsage);
            Assert.Equal(78.1, utilization.DiskUsage);
            Assert.True(utilization.NetworkBandwidth >= 0);
            Assert.NotEmpty(utilization.ProcessMetrics);
        }

        [Fact]
        public async Task DetectPerformanceAnomaliesAsync_IdentifiesIssues()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateAnomalousPerformanceData(organizationId);

            // Act
            var anomalies = await _service.DetectPerformanceAnomaliesAsync(organizationId);

            // Assert
            Assert.NotEmpty(anomalies);
            Assert.Contains(anomalies, a => a.Type == AnomalyType.LatencySpike);
            Assert.Contains(anomalies, a => a.Severity == AnomalySeverity.High);
            Assert.All(anomalies, a => Assert.True(a.Confidence > 0.7));
        }

        #endregion

        #region User Analytics Tests

        [Fact]
        public async Task GetUserBehaviorAnalyticsAsync_TracksUserPatterns()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateUserActivityData(organizationId);

            // Act
            var analytics = await _service.GetUserBehaviorAnalyticsAsync(organizationId);

            // Assert
            Assert.NotNull(analytics);
            Assert.NotEmpty(analytics.MostActiveUsers);
            Assert.NotEmpty(analytics.CommonActions);
            Assert.NotEmpty(analytics.UserJourneys);
            Assert.True(analytics.AverageActionsPerSession > 0);
            Assert.True(analytics.BounceRate >= 0 && analytics.BounceRate <= 100);
        }

        [Fact]
        public async Task GetUserEngagementMetricsAsync_MeasuresEngagement()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await CreateEngagementData(organizationId, userId);

            // Act
            var metrics = await _service.GetUserEngagementMetricsAsync(organizationId, userId);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.DailyActiveRate > 0);
            Assert.True(metrics.WeeklyActiveRate >= metrics.DailyActiveRate);
            Assert.True(metrics.MonthlyActiveRate >= metrics.WeeklyActiveRate);
            Assert.True(metrics.RetentionRate > 0);
            Assert.NotEmpty(metrics.EngagementTrend);
        }

        [Fact]
        public async Task PredictUserChurnAsync_IdentifiesAtRiskUsers()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateChurnIndicatorData(organizationId);

            // Act
            var predictions = await _service.PredictUserChurnAsync(organizationId);

            // Assert
            Assert.NotEmpty(predictions);
            Assert.All(predictions, p => 
            {
                Assert.True(p.ChurnProbability >= 0 && p.ChurnProbability <= 1);
                Assert.NotEmpty(p.RiskFactors);
                Assert.NotNull(p.RecommendedAction);
            });
            Assert.Contains(predictions, p => p.ChurnProbability > 0.7); // High risk users
        }

        #endregion

        #region Business Analytics Tests

        [Fact]
        public async Task GetBusinessMetricsAsync_ProvidesKPIs()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateBusinessData(organizationId);

            // Act
            var metrics = await _service.GetBusinessMetricsAsync(organizationId);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalRevenue > 0);
            Assert.True(metrics.AverageRevenuePerUser > 0);
            Assert.True(metrics.CustomerAcquisitionCost > 0);
            Assert.True(metrics.LifetimeValue > metrics.CustomerAcquisitionCost);
            Assert.True(metrics.MonthlyRecurringRevenue > 0);
            Assert.NotEmpty(metrics.RevenueByProduct);
        }

        [Fact]
        public async Task GetConversionFunnelAsync_TracksConversions()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateConversionData(organizationId);

            // Act
            var funnel = await _service.GetConversionFunnelAsync(organizationId);

            // Assert
            Assert.NotNull(funnel);
            Assert.NotEmpty(funnel.Stages);
            Assert.True(funnel.OverallConversionRate > 0);
            
            // Verify funnel progression (each stage should have fewer users)
            for (int i = 1; i < funnel.Stages.Count; i++)
            {
                Assert.True(funnel.Stages[i].Users <= funnel.Stages[i - 1].Users);
            }
        }

        #endregion

        #region Alert and Monitoring Tests

        [Fact]
        public async Task CheckThresholdAlertsAsync_TriggersAlerts()
        {
            // Arrange
            _metricsMock.Setup(m => m.GetGaugeValue("cpu_usage", It.IsAny<Dictionary<string, string>>()))
                .Returns(85.0); // Above threshold

            // Act
            var alerts = await _service.CheckThresholdAlertsAsync();

            // Assert
            Assert.NotEmpty(alerts);
            Assert.Contains(alerts, a => a.MetricName == "CpuUsage");
            Assert.Contains(alerts, a => a.Severity == AlertSeverity.Warning);
            Assert.All(alerts, a => Assert.NotNull(a.Message));
        }

        [Fact]
        public async Task CreateCustomAlertAsync_ConfiguresAlert()
        {
            // Arrange
            var alert = new CustomAlert
            {
                Name = "High Error Rate",
                MetricName = "error_rate",
                Condition = AlertCondition.GreaterThan,
                Threshold = 10.0,
                Duration = TimeSpan.FromMinutes(5),
                Severity = AlertSeverity.Critical
            };

            // Act
            var created = await _service.CreateCustomAlertAsync(alert);

            // Assert
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal(alert.Name, created.Name);
            Assert.True(created.IsActive);
        }

        #endregion

        #region Dashboard and Reporting Tests

        [Fact]
        public async Task GenerateExecutiveDashboardAsync_ProvidesOverview()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateComprehensiveData(organizationId);

            // Act
            var dashboard = await _service.GenerateExecutiveDashboardAsync(organizationId);

            // Assert
            Assert.NotNull(dashboard);
            Assert.NotEmpty(dashboard.KeyMetrics);
            Assert.NotEmpty(dashboard.Charts);
            Assert.NotEmpty(dashboard.Insights);
            Assert.NotNull(dashboard.Summary);
            Assert.True(dashboard.GeneratedAt > DateTime.UtcNow.AddSeconds(-10));
        }

        [Fact]
        public async Task ScheduleReportAsync_CreatesRecurringReport()
        {
            // Arrange
            var report = new ScheduledReport
            {
                Name = "Weekly Performance Report",
                Type = ReportType.Performance,
                Schedule = ReportSchedule.Weekly,
                Recipients = new[] { "admin@example.com" },
                OrganizationId = Guid.NewGuid()
            };

            // Act
            var scheduled = await _service.ScheduleReportAsync(report);

            // Assert
            Assert.NotNull(scheduled);
            Assert.NotEqual(Guid.Empty, scheduled.Id);
            Assert.True(scheduled.IsActive);
            Assert.NotNull(scheduled.NextRunTime);
            Assert.True(scheduled.NextRunTime > DateTime.UtcNow);
        }

        [Fact]
        public async Task GenerateCustomReportAsync_CreatesDetailedReport()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateComprehensiveData(organizationId);
            
            var request = new CustomReportRequest
            {
                OrganizationId = organizationId,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                Metrics = new[] { "Sessions", "Performance", "Users" },
                Format = RemoteC.Shared.Models.ReportFormat.Pdf,
                IncludeCharts = true
            };

            // Act
            var report = await _service.GenerateCustomReportAsync(request);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.Data.Length > 0);
            Assert.Equal(RemoteC.Shared.Models.ReportFormat.Pdf, report.Format);
            Assert.Contains("Sessions", report.Sections);
            Assert.Contains("Performance", report.Sections);
            Assert.Contains("Users", report.Sections);
        }

        #endregion

        #region Data Export Tests

        [Fact]
        public async Task ExportAnalyticsDataAsync_ProducesValidExport()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            await CreateComprehensiveData(organizationId);
            
            var exportRequest = new RemoteC.Shared.Models.DataExportRequest
            {
                OrganizationId = organizationId,
                DataTypes = new[] { "Sessions", "Performance", "Users" },
                Format = RemoteC.Shared.Models.ExportFormat.Csv,
                DateRange = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)
            };

            // Act
            var export = await _service.ExportAnalyticsDataAsync(exportRequest);

            // Assert
            Assert.NotNull(export);
            Assert.True(export.FileSize > 0);
            Assert.Equal(RemoteC.Shared.Models.ExportFormat.Csv, export.Format);
            Assert.NotNull(export.DownloadUrl);
            Assert.True(export.ExpiresAt > DateTime.UtcNow);
        }

        #endregion

        #region Helper Methods

        private async Task CreateSessionData(Guid organizationId, int count)
        {
            var random = new Random();
            var sessions = new List<Session>();
            
            for (int i = 0; i < count; i++)
            {
                var startTime = DateTime.UtcNow.AddDays(-random.Next(7)).AddHours(-random.Next(24));
                sessions.Add(new Session
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = Guid.NewGuid(),
                    DeviceId = Guid.NewGuid(),
                    StartedAt = startTime,
                    EndedAt = startTime.AddMinutes(random.Next(5, 120)),
                    Status = RemoteC.Shared.Models.SessionStatus.Ended
                });
            }

            _context.Sessions.AddRange(sessions);
            await _context.SaveChangesAsync();
        }

        private async Task CreateActiveSessions(Guid organizationId, int count)
        {
            var sessions = Enumerable.Range(0, count).Select(i => new Session
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = Guid.NewGuid(),
                DeviceId = Guid.NewGuid(),
                StartedAt = DateTime.UtcNow.AddMinutes(-i),
                Status = (RemoteC.Data.Entities.SessionStatus)RemoteC.Shared.Models.SessionStatus.Active,
                Location = i % 2 == 0 ? "US" : "EU",
                DeviceType = i % 3 == 0 ? "Desktop" : "Mobile"
            });

            _context.Sessions.AddRange(sessions);
            await _context.SaveChangesAsync();
        }

        private async Task CreateTrendingSessionData(Guid organizationId)
        {
            var sessions = new List<Session>();
            
            // Create increasing trend with peak on Mondays
            for (int week = 0; week < 4; week++)
            {
                for (int day = 0; day < 7; day++)
                {
                    var date = DateTime.UtcNow.AddDays(-(week * 7 + day));
                    var baseCount = 100 + (4 - week) * 20; // Increasing trend
                    var dayMultiplier = day == 1 ? 1.5 : 1.0; // Monday peak
                    
                    var dailySessions = (int)(baseCount * dayMultiplier);
                    
                    for (int i = 0; i < dailySessions; i++)
                    {
                        sessions.Add(new Session
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = organizationId,
                            StartedAt = date.AddHours(9 + i % 8), // Business hours
                            EndedAt = date.AddHours(9 + i % 8).AddMinutes(30),
                            Status = RemoteC.Shared.Models.SessionStatus.Ended
                        });
                    }
                }
            }

            _context.Sessions.AddRange(sessions);
            await _context.SaveChangesAsync();
        }

        private async Task CreatePerformanceData(Guid organizationId)
        {
            var metrics = new List<PerformanceMetric>();
            var random = new Random();
            
            for (int i = 0; i < 1000; i++)
            {
                metrics.Add(new PerformanceMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Latency = random.Next(20, 200),
                    Throughput = random.Next(100, 1000),
                    ErrorCount = random.Next(0, 10),
                    RequestCount = random.Next(50, 500)
                });
            }

            _context.PerformanceMetrics.AddRange(metrics);
            await _context.SaveChangesAsync();
        }

        private async Task CreateAnomalousPerformanceData(Guid organizationId)
        {
            var metrics = new List<PerformanceMetric>();
            
            // Normal baseline
            for (int i = 0; i < 100; i++)
            {
                metrics.Add(new PerformanceMetric
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Timestamp = DateTime.UtcNow.AddHours(-i),
                    Latency = 50 + new Random().Next(-10, 10),
                    ErrorCount = new Random().Next(0, 2)
                });
            }

            // Add anomalies
            metrics.Add(new PerformanceMetric
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow.AddHours(-5),
                Latency = 500, // Spike
                ErrorCount = 50
            });

            _context.PerformanceMetrics.AddRange(metrics);
            await _context.SaveChangesAsync();
        }

        private async Task CreateUserActivityData(Guid organizationId)
        {
            var users = Enumerable.Range(0, 20).Select(i => new User
            {
                Id = Guid.NewGuid(),
                Email = $"user{i}@example.com",
                OrganizationId = organizationId
            }).ToList();

            _context.Users.AddRange(users);

            var activities = new List<UserActivityLog>();
            var random = new Random();
            
            foreach (var user in users.Take(10)) // Top 10 active users
            {
                for (int i = 0; i < random.Next(50, 200); i++)
                {
                    activities.Add(new UserActivityLog
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Action = new[] { "Login", "ViewDashboard", "StartSession", "EndSession" }[random.Next(4)],
                        Timestamp = DateTime.UtcNow.AddDays(-random.Next(30))
                    });
                }
            }

            _context.UserActivityLogs.AddRange(activities);
            await _context.SaveChangesAsync();
        }

        private async Task CreateEngagementData(Guid organizationId, Guid userId)
        {
            var activities = new List<UserActivityLog>();
            
            // Daily active for last week
            for (int i = 0; i < 7; i++)
            {
                activities.Add(new UserActivityLog
                {
                    UserId = userId,
                    Action = "Login",
                    Timestamp = DateTime.UtcNow.AddDays(-i)
                });
            }

            // Weekly active for last month
            for (int i = 0; i < 4; i++)
            {
                activities.Add(new UserActivityLog
                {
                    UserId = userId,
                    Action = "StartSession",
                    Timestamp = DateTime.UtcNow.AddWeeks(-i)
                });
            }

            _context.UserActivityLogs.AddRange(activities);
            await _context.SaveChangesAsync();
        }

        private async Task CreateChurnIndicatorData(Guid organizationId)
        {
            var users = new List<User>();
            
            // Active users
            for (int i = 0; i < 10; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"active{i}@example.com",
                    OrganizationId = organizationId,
                    LastLoginDate = DateTime.UtcNow.AddDays(-i)
                };
                users.Add(user);
            }

            // At-risk users (no recent activity)
            for (int i = 0; i < 5; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"atrisk{i}@example.com",
                    OrganizationId = organizationId,
                    LastLoginDate = DateTime.UtcNow.AddDays(-45 - i * 10)
                };
                users.Add(user);
            }

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }

        private async Task CreateBusinessData(Guid organizationId)
        {
            // TODO: Subscription entity needs to be created for this test
            // // Create subscriptions
            // var subscriptions = Enumerable.Range(0, 100).Select(i => new Subscription
            // {
            //     Id = Guid.NewGuid(),
            //     OrganizationId = organizationId,
            //     UserId = Guid.NewGuid(),
            //     PlanName = i % 3 == 0 ? "Enterprise" : i % 2 == 0 ? "Professional" : "Basic",
            //     MonthlyPrice = i % 3 == 0 ? 999.99m : i % 2 == 0 ? 299.99m : 99.99m,
            //     StartDate = DateTime.UtcNow.AddMonths(-i % 12),
            //     Status = i % 10 == 0 ? "Cancelled" : "Active"
            // });

            // _context.Subscriptions.AddRange(subscriptions);
            // await _context.SaveChangesAsync();
            await Task.CompletedTask; // Placeholder
        }

        private async Task CreateConversionData(Guid organizationId)
        {
            // TODO: ConversionEvent entity needs to be created for this test
            // var funnelStages = new[]
            // {
            //     ("Visit", 1000),
            //     ("SignUp", 400),
            //     ("Trial", 200),
            //     ("Paid", 50)
            // };

            // foreach (var (stage, count) in funnelStages)
            // {
            //     var events = Enumerable.Range(0, count).Select(i => new ConversionEvent
            //     {
            //         Id = Guid.NewGuid(),
            //         OrganizationId = organizationId,
            //         UserId = Guid.NewGuid(),
            //         Stage = stage,
            //         Timestamp = DateTime.UtcNow.AddDays(-i % 30)
            //     });

            //     _context.ConversionEvents.AddRange(events);
            // }

            // await _context.SaveChangesAsync();
            await Task.CompletedTask; // Placeholder
        }

        private async Task CreateComprehensiveData(Guid organizationId)
        {
            await CreateSessionData(organizationId, 100);
            await CreatePerformanceData(organizationId);
            await CreateUserActivityData(organizationId);
            await CreateBusinessData(organizationId);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #endregion
    }

    // Test models for analytics
    public enum AnomalyType
    {
        LatencySpike,
        ErrorRateIncrease,
        TrafficDrop,
        UnusualActivity
    }

    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum AlertCondition
    {
        GreaterThan,
        LessThan,
        Equals,
        NotEquals
    }

    public enum ReportType
    {
        Performance,
        Usage,
        Security,
        Executive
    }

    public enum ReportSchedule
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly
    }

    // Removed duplicate ReportFormat enum - using RemoteC.Shared.Models.ReportFormat


    // Additional test entities - removed duplicate PerformanceMetric (using RemoteC.Data.Entities.PerformanceMetric)

    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ConversionEvent
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // Extension for date calculations
    public static class DateTimeExtensions
    {
        public static DateTime AddWeeks(this DateTime date, int weeks)
        {
            return date.AddDays(weeks * 7);
        }
    }
}