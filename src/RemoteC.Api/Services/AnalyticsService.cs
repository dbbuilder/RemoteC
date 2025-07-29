using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly RemoteCDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly ICacheService _cacheService;
        private readonly AnalyticsOptions _options;

        public AnalyticsService(
            RemoteCDbContext context,
            ILogger<AnalyticsService> logger,
            IMetricsCollector metricsCollector,
            ICacheService cacheService,
            IOptions<AnalyticsOptions> options)
        {
            _context = context;
            _logger = logger;
            _metricsCollector = metricsCollector;
            _cacheService = cacheService;
            _options = options.Value;
        }

        #region Session Analytics

        public async Task<SessionAnalytics> GetSessionAnalyticsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
        {
            var cacheKey = $"session_analytics:{organizationId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
            
            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var sessions = await _context.Sessions
                    .Where(s => s.OrganizationId == organizationId 
                        && s.CreatedAt >= startDate 
                        && s.CreatedAt <= endDate)
                    .ToListAsync();

                var sessionLogs = await _context.SessionLogs
                    .Where(sl => sl.OrganizationId == organizationId 
                        && sl.Timestamp >= startDate 
                        && sl.Timestamp <= endDate)
                    .ToListAsync();

                var analytics = new SessionAnalytics
                {
                    TotalSessions = sessions.Count,
                    UniqueSessions = sessions.Select(s => s.Id).Distinct().Count(),
                    UniqueUsers = sessions.Where(s => s.CreatedBy != Guid.Empty).Select(s => s.CreatedBy).Distinct().Count(),
                    CompletionRate = sessions.Count > 0 ? 
                        sessions.Count(s => s.Status == RemoteC.Data.Entities.SessionStatus.Completed) * 100.0 / sessions.Count : 0
                };

                // Calculate average duration
                var completedSessions = sessions.Where(s => s.EndedAt.HasValue && s.Status == RemoteC.Data.Entities.SessionStatus.Completed).ToList();
                if (completedSessions.Any())
                {
                    var durations = completedSessions.Select(s => s.EndedAt!.Value - s.CreatedAt).ToList();
                    analytics.AverageSessionDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
                    analytics.TotalSessionTime = TimeSpan.FromMilliseconds(durations.Sum(d => d.TotalMilliseconds));
                }

                // Sessions by day
                analytics.SessionsByDay = sessions
                    .GroupBy(s => s.CreatedAt.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Sessions by hour
                analytics.SessionsByHour = sessions
                    .GroupBy(s => s.CreatedAt.Hour)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Sessions by location
                analytics.SessionsByLocation = sessionLogs
                    .Where(sl => !string.IsNullOrEmpty(sl.Location))
                    .GroupBy(sl => sl.Location!)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Sessions by device
                analytics.SessionsByDevice = sessionLogs
                    .Where(sl => !string.IsNullOrEmpty(sl.DeviceType))
                    .GroupBy(sl => sl.DeviceType!)
                    .ToDictionary(g => g.Key, g => g.Count());

                return analytics;
            }, TimeSpan.FromMinutes(_options.AggregationIntervalMinutes));
        }

        public async Task<RealTimeSessionMetrics> GetRealTimeSessionMetricsAsync(Guid organizationId)
        {
            var metrics = new RealTimeSessionMetrics
            {
                Timestamp = DateTime.UtcNow
            };

            // Get active sessions
            var activeSessions = await _context.Sessions
                .Where(s => s.OrganizationId == organizationId 
                    && s.Status == RemoteC.Data.Entities.SessionStatus.Active)
                .ToListAsync();

            metrics.ActiveSessions = activeSessions.Count();
            metrics.ConcurrentUsers = activeSessions.Select(s => s.CreatedBy).Distinct().Count();

            // Sessions per minute (last 5 minutes)
            var recentSessions = await _context.Sessions
                .Where(s => s.OrganizationId == organizationId 
                    && s.CreatedAt >= DateTime.UtcNow.AddMinutes(-5))
                .CountAsync();
            metrics.SessionsPerMinute = recentSessions / 5.0;

            // Get recent session logs for location and device info
            var recentLogs = await _context.SessionLogs
                .Where(sl => sl.OrganizationId == organizationId 
                    && sl.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .ToListAsync();

            metrics.ActiveUsersByLocation = recentLogs
                .Where(sl => !string.IsNullOrEmpty(sl.Location))
                .GroupBy(sl => sl.Location!)
                .ToDictionary(g => g.Key, g => g.Count());

            metrics.DeviceTypes = recentLogs
                .Where(sl => !string.IsNullOrEmpty(sl.DeviceType))
                .GroupBy(sl => sl.DeviceType!)
                .ToDictionary(g => g.Key, g => g.Count());

            // Average latency from recent logs
            var latencyLogs = recentLogs.Where(sl => sl.Latency.HasValue).ToList();
            if (latencyLogs.Any())
            {
                metrics.AverageLatency = latencyLogs.Average(sl => sl.Latency!.Value);
            }

            return metrics;
        }

        public async Task<SessionTrends> GetSessionTrendsAsync(Guid organizationId, int days)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);
            
            var sessions = await _context.Sessions
                .Where(s => s.OrganizationId == organizationId 
                    && s.CreatedAt >= startDate 
                    && s.CreatedAt <= endDate)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            var trends = new SessionTrends();

            // Calculate growth rate
            var firstWeekCount = sessions.Count(s => s.CreatedAt < startDate.AddDays(7));
            var lastWeekCount = sessions.Count(s => s.CreatedAt >= endDate.AddDays(-7));
            
            if (firstWeekCount > 0)
            {
                trends.GrowthRate = ((double)(lastWeekCount - firstWeekCount) / firstWeekCount) * 100;
            }

            // Find peak hours
            var hourGroups = sessions
                .GroupBy(s => s.CreatedAt.Hour)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .ToList();
            
            trends.PeakHours = hourGroups.Select(g => $"{g.Key:00}:00").ToList();

            // Find peak days
            var dayGroups = sessions
                .GroupBy(s => s.CreatedAt.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .ToList();
            
            trends.PeakDays = dayGroups.Select(g => g.Key.ToString()).ToList();

            // Historical trend
            trends.HistoricalTrend = sessions
                .GroupBy(s => s.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // Predict next week (simple linear regression)
            if (sessions.Count >= 7)
            {
                var dailyCounts = trends.HistoricalTrend.Values.ToList();
                var avgDailyGrowth = dailyCounts.Count > 1 ? 
                    (dailyCounts.Last() - dailyCounts.First()) / (double)(dailyCounts.Count - 1) : 0;
                
                var lastWeekAvg = dailyCounts.TakeLast(7).Average();
                trends.PredictedNextWeekSessions = (int)(lastWeekAvg * 7 * (1 + avgDailyGrowth / 100));
            }

            // Determine trend direction
            trends.TrendDirection = trends.GrowthRate > 5 ? "Increasing" : 
                                   trends.GrowthRate < -5 ? "Decreasing" : "Stable";

            // Calculate seasonality index (simplified)
            if (dayGroups.Any())
            {
                var maxDayCount = dayGroups.First().Count();
                var avgDayCount = sessions.Count / 7.0;
                trends.SeasonalityIndex = maxDayCount / avgDayCount;
            }

            return trends;
        }

        #endregion

        #region Performance Analytics

        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(Guid organizationId)
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-1);

            var performanceData = await _context.PerformanceMetrics
                .Where(pm => pm.OrganizationId == organizationId 
                    && pm.Timestamp >= startTime 
                    && pm.Timestamp <= endTime)
                .ToListAsync();

            var metrics = new PerformanceMetrics
            {
                MeasurementPeriodStart = startTime,
                MeasurementPeriodEnd = endTime
            };

            if (!performanceData.Any())
            {
                return metrics;
            }

            // Calculate latency metrics
            var latencies = performanceData
                .Where(pm => pm.Latency.HasValue)
                .Select(pm => pm.Latency!.Value)
                .OrderBy(l => l)
                .ToList();

            if (latencies.Any())
            {
                metrics.AverageLatency = latencies.Average();
                metrics.P50Latency = GetPercentile(latencies, 50);
                metrics.P95Latency = GetPercentile(latencies, 95);
                metrics.P99Latency = GetPercentile(latencies, 99);

                // Create histogram
                metrics.LatencyHistogram = CreateHistogram(latencies, 10);
            }

            // Calculate throughput
            var throughputData = performanceData.Where(pm => pm.Throughput.HasValue).ToList();
            if (throughputData.Any())
            {
                metrics.Throughput = throughputData.Average(pm => pm.Throughput!.Value);
            }

            // Calculate error rate
            var totalRequests = performanceData.Sum(pm => pm.RequestCount ?? 0);
            var totalErrors = performanceData.Sum(pm => pm.ErrorCount ?? 0);
            
            if (totalRequests > 0)
            {
                metrics.ErrorRate = (totalErrors * 100.0) / totalRequests;
                metrics.SuccessRate = 100 - metrics.ErrorRate;
            }

            // Endpoint metrics
            var endpointGroups = performanceData
                .Where(pm => !string.IsNullOrEmpty(pm.Endpoint) && pm.Latency.HasValue)
                .GroupBy(pm => pm.Endpoint!)
                .ToList();

            metrics.EndpointMetrics = endpointGroups.ToDictionary(
                g => g.Key,
                g => g.Average(pm => pm.Latency!.Value)
            );

            return metrics;
        }

        public async Task<ResourceUtilization> GetResourceUtilizationAsync(string serverId)
        {
            var utilization = new ResourceUtilization
            {
                ServerId = serverId,
                Timestamp = DateTime.UtcNow
            };

            // Get metrics from metrics collector
            var tags = new Dictionary<string, string> { ["server"] = serverId };
            
            utilization.CpuUsage = _metricsCollector.GetGaugeValue("cpu_usage", tags);
            utilization.MemoryUsage = _metricsCollector.GetGaugeValue("memory_usage", tags);
            utilization.DiskUsage = _metricsCollector.GetGaugeValue("disk_usage", tags);
            utilization.NetworkIn = _metricsCollector.GetGaugeValue("network_in", tags);
            utilization.NetworkOut = _metricsCollector.GetGaugeValue("network_out", tags);
            utilization.NetworkBandwidth = utilization.NetworkIn + utilization.NetworkOut;

            // Get process metrics (mock for now)
            utilization.ProcessMetrics = new Dictionary<string, ProcessMetric>
            {
                ["remotec_api"] = new ProcessMetric
                {
                    ProcessName = "remotec_api",
                    CpuUsage = _metricsCollector.GetGaugeValue("process_cpu", new Dictionary<string, string> { ["process"] = "remotec_api" }),
                    MemoryUsage = _metricsCollector.GetGaugeValue("process_memory", new Dictionary<string, string> { ["process"] = "remotec_api" }),
                    ThreadCount = 25,
                    HandleCount = 150
                }
            };

            await Task.CompletedTask;
            return utilization;
        }

        public async Task<List<PerformanceAnomaly>> DetectPerformanceAnomaliesAsync(Guid organizationId)
        {
            var anomalies = new List<PerformanceAnomaly>();
            
            // Get recent performance data
            var recentData = await _context.PerformanceMetrics
                .Where(pm => pm.OrganizationId == organizationId 
                    && pm.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .OrderBy(pm => pm.Timestamp)
                .ToListAsync();

            // Get historical baseline (last 24 hours)
            var baselineData = await _context.PerformanceMetrics
                .Where(pm => pm.OrganizationId == organizationId 
                    && pm.Timestamp >= DateTime.UtcNow.AddDays(-1) 
                    && pm.Timestamp < DateTime.UtcNow.AddHours(-1))
                .ToListAsync();

            if (!recentData.Any() || !baselineData.Any())
            {
                return anomalies;
            }

            // Detect latency spikes
            var baselineLatencies = baselineData
                .Where(pm => pm.Latency.HasValue)
                .Select(pm => pm.Latency!.Value)
                .ToList();

            if (baselineLatencies.Any())
            {
                var baselineAvg = baselineLatencies.Average();
                var baselineStdDev = CalculateStandardDeviation(baselineLatencies);
                var threshold = baselineAvg + (2 * baselineStdDev); // 2 standard deviations

                var recentLatencySpikes = recentData
                    .Where(pm => pm.Latency.HasValue && pm.Latency.Value > threshold)
                    .ToList();

                foreach (var spike in recentLatencySpikes)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        Id = Guid.NewGuid(),
                        Type = AnomalyType.LatencySpike,
                        Severity = spike.Latency!.Value > baselineAvg * 3 ? AnomalySeverity.Critical : AnomalySeverity.High,
                        MetricName = "Latency",
                        ExpectedValue = baselineAvg,
                        ActualValue = spike.Latency.Value,
                        Confidence = CalculateAnomalyConfidence(spike.Latency.Value, baselineAvg, baselineStdDev),
                        DetectedAt = spike.Timestamp,
                        Description = $"Latency spike detected: {spike.Latency.Value:F2}ms (expected: {baselineAvg:F2}ms)",
                        Context = new Dictionary<string, object>
                        {
                            ["endpoint"] = spike.Endpoint ?? "unknown",
                            ["server"] = spike.ServerId ?? "unknown"
                        }
                    });
                }
            }

            // Detect error rate increases
            var baselineErrorRate = CalculateErrorRate(baselineData);
            var recentErrorRate = CalculateErrorRate(recentData);

            if (recentErrorRate > baselineErrorRate * 2 && recentErrorRate > 5) // At least 2x increase and > 5%
            {
                anomalies.Add(new PerformanceAnomaly
                {
                    Id = Guid.NewGuid(),
                    Type = AnomalyType.ErrorRateIncrease,
                    Severity = recentErrorRate > 20 ? AnomalySeverity.Critical : AnomalySeverity.High,
                    MetricName = "ErrorRate",
                    ExpectedValue = baselineErrorRate,
                    ActualValue = recentErrorRate,
                    Confidence = 0.85,
                    DetectedAt = DateTime.UtcNow,
                    Description = $"Error rate increased to {recentErrorRate:F2}% (baseline: {baselineErrorRate:F2}%)"
                });
            }

            return anomalies;
        }

        #endregion

        #region User Analytics

        public async Task<UserBehaviorAnalytics> GetUserBehaviorAnalyticsAsync(Guid organizationId)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var activities = await _context.UserActivityLogs
                .Include(ua => ua.User)
                .Where(ua => ua.OrganizationId == organizationId 
                    && ua.Timestamp >= startDate 
                    && ua.Timestamp <= endDate)
                .ToListAsync();

            var analytics = new UserBehaviorAnalytics();

            // Most active users
            analytics.MostActiveUsers = activities
                .GroupBy(ua => new { ua.UserId, ua.User.Email })
                .Select(g => new UserActivity
                {
                    UserId = g.Key.UserId,
                    UserEmail = g.Key.Email,
                    ActionCount = g.Count(),
                    LastActivity = g.Max(ua => ua.Timestamp),
                    TotalActiveTime = TimeSpan.FromMinutes(g.Count() * 5) // Estimate
                })
                .OrderByDescending(ua => ua.ActionCount)
                .Take(10)
                .ToList();

            // Common actions
            analytics.CommonActions = activities
                .GroupBy(ua => ua.Action)
                .ToDictionary(g => g.Key, g => g.Count());

            // User journeys (simplified)
            var sessionActivities = activities
                .Where(ua => ua.SessionId.HasValue)
                .GroupBy(ua => ua.SessionId!.Value)
                .ToList();

            var commonJourneys = new Dictionary<string, int>();
            foreach (var session in sessionActivities)
            {
                var journey = string.Join(" → ", session.OrderBy(a => a.Timestamp).Select(a => a.Action).Take(5));
                if (commonJourneys.ContainsKey(journey))
                    commonJourneys[journey]++;
                else
                    commonJourneys[journey] = 1;
            }

            analytics.UserJourneys = commonJourneys
                .OrderByDescending(j => j.Value)
                .Take(5)
                .Select(j => new UserJourney
                {
                    JourneyName = j.Key,
                    Steps = j.Key.Split(" → ").ToList(),
                    UserCount = j.Value,
                    CompletionRate = 100, // Simplified
                    AverageDuration = TimeSpan.FromMinutes(15) // Simplified
                })
                .ToList();

            // Average actions per session
            var sessionsWithActivities = activities
                .Where(ua => ua.SessionId.HasValue)
                .Select(ua => ua.SessionId!.Value)
                .Distinct()
                .Count();

            analytics.AverageActionsPerSession = sessionsWithActivities > 0 ? 
                activities.Count(ua => ua.SessionId.HasValue) / (double)sessionsWithActivities : 0;

            // Bounce rate (sessions with only 1 action)
            var singleActionSessions = sessionActivities.Count(g => g.Count() == 1);
            analytics.BounceRate = sessionsWithActivities > 0 ? 
                (singleActionSessions * 100.0) / sessionsWithActivities : 0;

            // Activity by hour
            analytics.ActivityByHour = activities
                .GroupBy(ua => ua.Timestamp.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            // Feature adoption (simplified)
            analytics.FeatureAdoption = new Dictionary<string, double>
            {
                ["FileTransfer"] = activities.Count(a => a.Action.Contains("File")) * 100.0 / Math.Max(activities.Count, 1),
                ["RemoteControl"] = activities.Count(a => a.Action.Contains("Session")) * 100.0 / Math.Max(activities.Count, 1),
                ["Chat"] = activities.Count(a => a.Action.Contains("Chat")) * 100.0 / Math.Max(activities.Count, 1)
            };

            return analytics;
        }

        public async Task<UserEngagementMetrics> GetUserEngagementMetricsAsync(Guid organizationId, Guid? userId = null)
        {
            var metrics = new UserEngagementMetrics();
            var now = DateTime.UtcNow;

            IQueryable<UserActivityLog> query = _context.UserActivityLogs
                .Where(ua => ua.OrganizationId == organizationId);

            if (userId.HasValue)
            {
                query = query.Where(ua => ua.UserId == userId.Value);
            }

            // Daily active users
            var dailyActiveUsers = await query
                .Where(ua => ua.Timestamp >= now.AddDays(-1))
                .Select(ua => ua.UserId)
                .Distinct()
                .CountAsync();

            // Weekly active users
            var weeklyActiveUsers = await query
                .Where(ua => ua.Timestamp >= now.AddDays(-7))
                .Select(ua => ua.UserId)
                .Distinct()
                .CountAsync();

            // Monthly active users
            var monthlyActiveUsers = await query
                .Where(ua => ua.Timestamp >= now.AddDays(-30))
                .Select(ua => ua.UserId)
                .Distinct()
                .CountAsync();

            var totalUsers = await _context.Users
                .Where(u => u.OrganizationId == organizationId)
                .CountAsync();

            if (totalUsers > 0)
            {
                metrics.DailyActiveRate = (dailyActiveUsers * 100.0) / totalUsers;
                metrics.WeeklyActiveRate = (weeklyActiveUsers * 100.0) / totalUsers;
                metrics.MonthlyActiveRate = (monthlyActiveUsers * 100.0) / totalUsers;
            }

            // Stickiness (DAU/MAU)
            metrics.Stickiness = monthlyActiveUsers > 0 ? dailyActiveUsers / (double)monthlyActiveUsers : 0;

            // Retention rate (users active this week who were also active last week)
            var thisWeekUsers = await query
                .Where(ua => ua.Timestamp >= now.AddDays(-7))
                .Select(ua => ua.UserId)
                .Distinct()
                .ToListAsync();

            var lastWeekUsers = await query
                .Where(ua => ua.Timestamp >= now.AddDays(-14) && ua.Timestamp < now.AddDays(-7))
                .Select(ua => ua.UserId)
                .Distinct()
                .ToListAsync();

            var retainedUsers = thisWeekUsers.Intersect(lastWeekUsers).Count();
            metrics.RetentionRate = lastWeekUsers.Count > 0 ? (retainedUsers * 100.0) / lastWeekUsers.Count : 0;

            // Session frequency for specific user
            if (userId.HasValue)
            {
                var userSessions = await _context.Sessions
                    .Where(s => s.CreatedBy == userId.Value && s.CreatedAt >= now.AddDays(-30))
                    .CountAsync();
                metrics.SessionFrequency = userSessions;

                // Consecutive days active
                var userActivities = await query
                    .Where(ua => ua.UserId == userId.Value && ua.Timestamp >= now.AddDays(-30))
                    .Select(ua => ua.Timestamp.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToListAsync();

                metrics.ConsecutiveDaysActive = CalculateConsecutiveDays(userActivities);
            }

            // Engagement trend
            metrics.EngagementTrend = new Dictionary<DateTime, double>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i).Date;
                var activeOnDay = await query
                    .Where(ua => ua.Timestamp >= date && ua.Timestamp < date.AddDays(1))
                    .Select(ua => ua.UserId)
                    .Distinct()
                    .CountAsync();

                metrics.EngagementTrend[date] = totalUsers > 0 ? (activeOnDay * 100.0) / totalUsers : 0;
            }

            return metrics;
        }

        public async Task<List<ChurnPrediction>> PredictUserChurnAsync(Guid organizationId)
        {
            var predictions = new List<ChurnPrediction>();
            var now = DateTime.UtcNow;

            // Get all users with their last activity
            var userActivities = await _context.Users
                .Where(u => u.OrganizationId == organizationId)
                .GroupJoin(
                    _context.UserActivityLogs,
                    u => u.Id,
                    ua => ua.UserId,
                    (u, activities) => new
                    {
                        User = u,
                        LastActivity = activities.OrderByDescending(a => a.Timestamp).FirstOrDefault()
                    })
                .ToListAsync();

            foreach (var userActivity in userActivities)
            {
                var daysSinceLastActivity = userActivity.LastActivity != null ? 
                    (now - userActivity.LastActivity.Timestamp).Days : 365;

                var riskFactors = new List<string>();
                double churnProbability = 0;

                // No activity in last 30 days
                if (daysSinceLastActivity > 30)
                {
                    riskFactors.Add("No activity in 30+ days");
                    churnProbability += 0.4;
                }
                // Declining activity
                else if (daysSinceLastActivity > 14)
                {
                    riskFactors.Add("Declining activity");
                    churnProbability += 0.2;
                }

                // Check session frequency decline
                var recentSessions = await _context.Sessions
                    .Where(s => s.CreatedBy == userActivity.User.Id && s.CreatedAt >= now.AddDays(-30))
                    .CountAsync();

                var previousSessions = await _context.Sessions
                    .Where(s => s.CreatedBy == userActivity.User.Id 
                        && s.CreatedAt >= now.AddDays(-60) 
                        && s.CreatedAt < now.AddDays(-30))
                    .CountAsync();

                if (previousSessions > 0 && recentSessions < previousSessions * 0.5)
                {
                    riskFactors.Add("50%+ decline in session frequency");
                    churnProbability += 0.3;
                }

                // Never completed onboarding (no completed sessions)
                var hasCompletedSessions = await _context.Sessions
                    .AnyAsync(s => s.CreatedBy == userActivity.User.Id && s.Status == RemoteC.Data.Entities.SessionStatus.Completed);

                if (!hasCompletedSessions)
                {
                    riskFactors.Add("Never completed a session");
                    churnProbability += 0.1;
                }

                // Normalize probability
                churnProbability = Math.Min(churnProbability, 0.95);

                if (churnProbability > 0.3) // Only include users with meaningful churn risk
                {
                    var riskLevel = churnProbability > 0.7 ? ChurnRiskLevel.Critical :
                                   churnProbability > 0.5 ? ChurnRiskLevel.High :
                                   churnProbability > 0.3 ? ChurnRiskLevel.Medium : ChurnRiskLevel.Low;

                    predictions.Add(new ChurnPrediction
                    {
                        UserId = userActivity.User.Id,
                        UserEmail = userActivity.User.Email,
                        ChurnProbability = churnProbability,
                        RiskFactors = riskFactors,
                        PredictionDate = now,
                        DaysSinceLastActivity = daysSinceLastActivity,
                        RiskLevel = riskLevel,
                        RecommendedAction = GetRecommendedAction(riskLevel, riskFactors)
                    });
                }
            }

            return predictions.OrderByDescending(p => p.ChurnProbability).ToList();
        }

        #endregion

        #region Business Analytics

        public async Task<BusinessMetrics> GetBusinessMetricsAsync(Guid organizationId)
        {
            var metrics = new BusinessMetrics();
            var now = DateTime.UtcNow;

            // Get business events
            var revenueEvents = await _context.BusinessEvents
                .Where(be => be.OrganizationId == organizationId 
                    && be.EventType == "Revenue" 
                    && be.Revenue.HasValue)
                .ToListAsync();

            metrics.TotalRevenue = revenueEvents.Sum(e => e.Revenue ?? 0);

            // Monthly recurring revenue (last 30 days)
            var monthlyRevenue = revenueEvents
                .Where(e => e.Timestamp >= now.AddDays(-30))
                .Sum(e => e.Revenue ?? 0);
            metrics.MonthlyRecurringRevenue = monthlyRevenue;

            // Get customer data
            var totalCustomers = await _context.Users
                .Where(u => u.OrganizationId == organizationId)
                .CountAsync();
            metrics.TotalCustomers = totalCustomers;

            // New customers (last 30 days)
            metrics.NewCustomers = await _context.Users
                .Where(u => u.OrganizationId == organizationId && u.CreatedAt >= now.AddDays(-30))
                .CountAsync();

            // Average revenue per user
            metrics.AverageRevenuePerUser = totalCustomers > 0 ? metrics.TotalRevenue / totalCustomers : 0;

            // Revenue by product
            metrics.RevenueByProduct = revenueEvents
                .Where(e => !string.IsNullOrEmpty(e.ProductName))
                .GroupBy(e => e.ProductName!)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Revenue ?? 0));

            // Customer acquisition cost (simplified - would need actual marketing spend data)
            metrics.CustomerAcquisitionCost = 50; // Placeholder

            // Lifetime value (simplified - based on average revenue and retention)
            var avgMonthlyRevenuePerUser = totalCustomers > 0 ? monthlyRevenue / totalCustomers : 0;
            var estimatedLifetimeMonths = 24; // Placeholder
            metrics.LifetimeValue = avgMonthlyRevenuePerUser * estimatedLifetimeMonths;

            // Churn rate (users with no activity in last 30 days)
            var activeUsers = await _context.UserActivityLogs
                .Where(ua => ua.OrganizationId == organizationId && ua.Timestamp >= now.AddDays(-30))
                .Select(ua => ua.UserId)
                .Distinct()
                .CountAsync();

            var inactiveUsers = totalCustomers - activeUsers;
            metrics.ChurnRate = totalCustomers > 0 ? (inactiveUsers * 100.0) / totalCustomers : 0;

            // Revenue growth trend
            metrics.RevenueGrowth = new Dictionary<DateTime, decimal>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                
                var monthRevenue = revenueEvents
                    .Where(e => e.Timestamp >= monthStart && e.Timestamp < monthEnd)
                    .Sum(e => e.Revenue ?? 0);
                
                metrics.RevenueGrowth[monthStart] = monthRevenue;
            }

            return metrics;
        }

        public async Task<ConversionFunnel> GetConversionFunnelAsync(Guid organizationId)
        {
            var funnel = new ConversionFunnel();
            
            // Define funnel stages
            var stageDefinitions = new[]
            {
                ("Visit", "PageView"),
                ("SignUp", "UserRegistered"),
                ("Trial", "TrialStarted"),
                ("Paid", "SubscriptionStarted")
            };

            var stages = new List<FunnelStage>();
            var previousStageUsers = new HashSet<Guid>();
            var isFirstStage = true;

            foreach (var (stageName, eventType) in stageDefinitions)
            {
                var stageEvents = await _context.BusinessEvents
                    .Where(be => be.OrganizationId == organizationId 
                        && be.EventType == eventType
                        && be.UserId.HasValue)
                    .ToListAsync();

                var stageUsers = stageEvents
                    .Where(e => e.UserId.HasValue)
                    .Select(e => e.UserId!.Value)
                    .Distinct()
                    .ToHashSet();

                var stage = new FunnelStage
                {
                    Name = stageName,
                    Users = stageUsers.Count
                };

                // Calculate conversion rate from previous stage
                if (!isFirstStage && previousStageUsers.Count > 0)
                {
                    var convertedUsers = stageUsers.Intersect(previousStageUsers).Count();
                    stage.ConversionRate = (convertedUsers * 100.0) / previousStageUsers.Count;
                    stage.ExitCount = previousStageUsers.Count - convertedUsers;
                }
                else
                {
                    stage.ConversionRate = 100; // First stage is 100%
                }

                // Calculate average time in stage (simplified)
                stage.AverageTimeInStage = TimeSpan.FromHours(24); // Placeholder

                stages.Add(stage);
                previousStageUsers = stageUsers;
                isFirstStage = false;
            }

            funnel.Stages = stages;

            // Overall conversion rate
            if (stages.Count >= 2 && stages[0].Users > 0)
            {
                funnel.OverallConversionRate = (stages.Last().Users * 100.0) / stages[0].Users;
            }

            // Stage dropoff rates
            funnel.StageDropoffRates = new Dictionary<string, double>();
            for (int i = 1; i < stages.Count; i++)
            {
                var dropoffRate = 100 - stages[i].ConversionRate;
                funnel.StageDropoffRates[$"{stages[i-1].Name} → {stages[i].Name}"] = dropoffRate;
            }

            // Average time to conversion
            funnel.AverageTimeToConversion = TimeSpan.FromDays(7); // Placeholder

            // Conversion by source (simplified)
            funnel.ConversionBySource = new Dictionary<string, double>
            {
                ["Direct"] = 25.0,
                ["Organic"] = 35.0,
                ["Paid"] = 30.0,
                ["Referral"] = 10.0
            };

            return funnel;
        }

        #endregion

        #region Alerts and Monitoring

        public async Task<List<Alert>> CheckThresholdAlertsAsync()
        {
            var alerts = new List<Alert>();

            foreach (var threshold in _options.AlertThresholds)
            {
                var currentValue = _metricsCollector.GetGaugeValue(threshold.Key.ToLower().Replace(" ", "_"));
                
                if (currentValue > threshold.Value)
                {
                    var severity = threshold.Key switch
                    {
                        "CpuUsage" when currentValue > 90 => AlertSeverity.Critical,
                        "MemoryUsage" when currentValue > 90 => AlertSeverity.Critical,
                        "ErrorRate" when currentValue > 10 => AlertSeverity.Critical,
                        _ => AlertSeverity.Warning
                    };

                    alerts.Add(new Alert
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{threshold.Key} Threshold Exceeded",
                        MetricName = threshold.Key,
                        CurrentValue = currentValue,
                        ThresholdValue = threshold.Value,
                        Severity = severity,
                        TriggeredAt = DateTime.UtcNow,
                        Message = $"{threshold.Key} is at {currentValue:F2}% (threshold: {threshold.Value}%)",
                        IsAcknowledged = false
                    });
                }
            }

            // Check custom alerts
            var customAlerts = await _context.CustomAlerts
                .Where(ca => ca.IsActive)
                .ToListAsync();

            foreach (var customAlert in customAlerts)
            {
                var currentValue = _metricsCollector.GetGaugeValue(customAlert.MetricName);
                var shouldTrigger = EvaluateAlertCondition(currentValue, customAlert.Threshold, customAlert.Condition);

                if (shouldTrigger)
                {
                    // Check if already triggered recently
                    var recentAlert = await _context.AlertHistory
                        .Where(ah => ah.AlertId == customAlert.Id 
                            && ah.TriggeredAt >= DateTime.UtcNow.AddMinutes(-customAlert.DurationMinutes))
                        .FirstOrDefaultAsync();

                    if (recentAlert == null)
                    {
                        var alert = new Alert
                        {
                            Id = Guid.NewGuid(),
                            Name = customAlert.Name,
                            MetricName = customAlert.MetricName,
                            CurrentValue = currentValue,
                            ThresholdValue = customAlert.Threshold,
                            Severity = Enum.Parse<AlertSeverity>(customAlert.Severity),
                            TriggeredAt = DateTime.UtcNow,
                            Message = customAlert.Description ?? $"{customAlert.Name} triggered",
                            Tags = string.IsNullOrEmpty(customAlert.Tags) ? 
                                new Dictionary<string, string>() : 
                                JsonSerializer.Deserialize<Dictionary<string, string>>(customAlert.Tags) ?? new Dictionary<string, string>(),
                            IsAcknowledged = false
                        };

                        alerts.Add(alert);

                        // Record in history
                        var history = new AlertHistory
                        {
                            AlertId = customAlert.Id,
                            OrganizationId = customAlert.OrganizationId,
                            MetricName = customAlert.MetricName,
                            CurrentValue = currentValue,
                            ThresholdValue = customAlert.Threshold,
                            Severity = customAlert.Severity,
                            TriggeredAt = DateTime.UtcNow,
                            Message = alert.Message
                        };

                        _context.AlertHistory.Add(history);
                        
                        // Update last triggered
                        customAlert.LastTriggered = DateTime.UtcNow;
                    }
                }
            }

            if (alerts.Any())
            {
                await _context.SaveChangesAsync();
            }

            return alerts;
        }

        public async Task<CustomAlert> CreateCustomAlertAsync(CustomAlert alert)
        {
            var entity = new CustomAlertEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = alert.OrganizationId,
                Name = alert.Name,
                Description = alert.Description,
                MetricName = alert.MetricName,
                Condition = alert.Condition.ToString(),
                Threshold = alert.Threshold,
                DurationMinutes = (int)alert.Duration.TotalMinutes,
                Severity = alert.Severity.ToString(),
                IsActive = alert.IsActive,
                NotificationChannels = JsonSerializer.Serialize(alert.NotificationChannels),
                Tags = alert.Tags.Any() ? JsonSerializer.Serialize(alert.Tags) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomAlerts.Add(entity);
            await _context.SaveChangesAsync();

            alert.Id = entity.Id;
            alert.CreatedAt = entity.CreatedAt;
            
            return alert;
        }

        public async Task<bool> UpdateAlertAsync(Guid alertId, CustomAlert alert)
        {
            var entity = await _context.CustomAlerts.FindAsync(alertId);
            if (entity == null)
            {
                return false;
            }

            entity.Name = alert.Name;
            entity.Description = alert.Description;
            entity.MetricName = alert.MetricName;
            entity.Condition = alert.Condition.ToString();
            entity.Threshold = alert.Threshold;
            entity.DurationMinutes = (int)alert.Duration.TotalMinutes;
            entity.Severity = alert.Severity.ToString();
            entity.IsActive = alert.IsActive;
            entity.NotificationChannels = JsonSerializer.Serialize(alert.NotificationChannels);
            entity.Tags = alert.Tags.Any() ? JsonSerializer.Serialize(alert.Tags) : null;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAlertAsync(Guid alertId)
        {
            var entity = await _context.CustomAlerts.FindAsync(alertId);
            if (entity == null)
            {
                return false;
            }

            _context.CustomAlerts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CustomAlert>> GetActiveAlertsAsync(Guid? organizationId = null)
        {
            var query = _context.CustomAlerts.Where(ca => ca.IsActive);
            
            if (organizationId.HasValue)
            {
                query = query.Where(ca => ca.OrganizationId == organizationId.Value);
            }

            var entities = await query.ToListAsync();

            return entities.Select(e => new CustomAlert
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                MetricName = e.MetricName,
                Condition = Enum.Parse<AlertCondition>(e.Condition),
                Threshold = e.Threshold,
                Duration = TimeSpan.FromMinutes(e.DurationMinutes),
                Severity = Enum.Parse<AlertSeverity>(e.Severity),
                IsActive = e.IsActive,
                NotificationChannels = string.IsNullOrEmpty(e.NotificationChannels) ? 
                    new List<string>() : 
                    JsonSerializer.Deserialize<List<string>>(e.NotificationChannels) ?? new List<string>(),
                Tags = string.IsNullOrEmpty(e.Tags) ? 
                    new Dictionary<string, string>() : 
                    JsonSerializer.Deserialize<Dictionary<string, string>>(e.Tags) ?? new Dictionary<string, string>(),
                CreatedAt = e.CreatedAt,
                LastTriggered = e.LastTriggered
            }).ToList();
        }

        #endregion

        #region Dashboards and Reporting

        public async Task<ExecutiveDashboard> GenerateExecutiveDashboardAsync(Guid organizationId)
        {
            var dashboard = new ExecutiveDashboard
            {
                GeneratedAt = DateTime.UtcNow,
                PeriodStart = DateTime.UtcNow.AddDays(-30),
                PeriodEnd = DateTime.UtcNow
            };

            // Get key metrics
            var sessionAnalytics = await GetSessionAnalyticsAsync(organizationId, dashboard.PeriodStart, dashboard.PeriodEnd);
            var performanceMetrics = await GetPerformanceMetricsAsync(organizationId);
            var businessMetrics = await GetBusinessMetricsAsync(organizationId);

            dashboard.KeyMetrics = new Dictionary<string, object>
            {
                ["TotalSessions"] = sessionAnalytics.TotalSessions,
                ["ActiveUsers"] = sessionAnalytics.UniqueUsers,
                ["AverageSessionDuration"] = sessionAnalytics.AverageSessionDuration.TotalMinutes,
                ["Revenue"] = businessMetrics.MonthlyRecurringRevenue,
                ["ChurnRate"] = businessMetrics.ChurnRate,
                ["SystemUptime"] = 99.9, // Placeholder
                ["AverageLatency"] = performanceMetrics.AverageLatency,
                ["ErrorRate"] = performanceMetrics.ErrorRate
            };

            // Create charts
            dashboard.Charts = new List<ChartData>
            {
                new ChartData
                {
                    Title = "Sessions Over Time",
                    Type = "line",
                    DataPoints = sessionAnalytics.SessionsByDay
                        .Select(kvp => new DataPoint
                        {
                            Label = kvp.Key.ToString("MMM dd"),
                            Value = kvp.Value,
                            Timestamp = kvp.Key
                        })
                        .ToList()
                },
                new ChartData
                {
                    Title = "Revenue Growth",
                    Type = "bar",
                    DataPoints = businessMetrics.RevenueGrowth
                        .Select(kvp => new DataPoint
                        {
                            Label = kvp.Key.ToString("MMM yyyy"),
                            Value = (double)kvp.Value,
                            Timestamp = kvp.Key
                        })
                        .ToList()
                },
                new ChartData
                {
                    Title = "User Activity by Hour",
                    Type = "heatmap",
                    DataPoints = sessionAnalytics.SessionsByHour
                        .Select(kvp => new DataPoint
                        {
                            Label = $"{kvp.Key:00}:00",
                            Value = kvp.Value
                        })
                        .ToList()
                }
            };

            // Generate insights
            dashboard.Insights = new List<string>();
            
            if (businessMetrics.ChurnRate > 10)
            {
                dashboard.Insights.Add($"High churn rate detected: {businessMetrics.ChurnRate:F1}%. Consider engagement campaigns.");
            }
            
            if (performanceMetrics.ErrorRate > 5)
            {
                dashboard.Insights.Add($"Error rate is elevated at {performanceMetrics.ErrorRate:F1}%. Investigation recommended.");
            }
            
            if (sessionAnalytics.SessionsByDay.Any())
            {
                var trend = sessionAnalytics.SessionsByDay.Values.TakeLast(7).Average() - 
                           sessionAnalytics.SessionsByDay.Values.Take(7).Average();
                if (trend > 0)
                {
                    dashboard.Insights.Add($"Session volume is trending up by {trend:F0} sessions/day.");
                }
            }

            // Comparisons vs previous period
            var previousPeriodSessions = await GetSessionAnalyticsAsync(
                organizationId, 
                dashboard.PeriodStart.AddDays(-30), 
                dashboard.PeriodStart);
            
            dashboard.Comparisons = new Dictionary<string, double>
            {
                ["SessionGrowth"] = sessionAnalytics.TotalSessions > 0 ? 
                    ((sessionAnalytics.TotalSessions - previousPeriodSessions.TotalSessions) * 100.0) / previousPeriodSessions.TotalSessions : 0,
                ["UserGrowth"] = ((businessMetrics.TotalCustomers - businessMetrics.TotalCustomers + businessMetrics.NewCustomers) * 100.0) / 
                    Math.Max(businessMetrics.TotalCustomers - businessMetrics.NewCustomers, 1)
            };

            // Summary
            dashboard.Summary = $"In the past 30 days, your organization had {sessionAnalytics.TotalSessions:N0} sessions " +
                              $"from {sessionAnalytics.UniqueUsers:N0} unique users. " +
                              $"Revenue reached ${businessMetrics.MonthlyRecurringRevenue:N2} with a churn rate of {businessMetrics.ChurnRate:F1}%. " +
                              $"System performance remains stable with {performanceMetrics.AverageLatency:F0}ms average latency.";

            return dashboard;
        }

        public async Task<ScheduledReport> ScheduleReportAsync(ScheduledReport report)
        {
            var entity = new ScheduledReportEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = report.OrganizationId,
                Name = report.Name,
                Description = report.Description,
                ReportType = report.Type.ToString(),
                Schedule = report.Schedule.ToString(),
                Recipients = JsonSerializer.Serialize(report.Recipients),
                IsActive = report.IsActive,
                Parameters = report.Parameters.Any() ? JsonSerializer.Serialize(report.Parameters) : null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty // Should be set from current user context
            };

            // Calculate next run time
            entity.NextRunTime = CalculateNextRunTime(report.Schedule);

            _context.ScheduledReports.Add(entity);
            await _context.SaveChangesAsync();

            report.Id = entity.Id;
            report.NextRunTime = entity.NextRunTime;
            
            return report;
        }

        public async Task<CustomReport> GenerateCustomReportAsync(CustomReportRequest request)
        {
            var report = new CustomReport
            {
                Id = Guid.NewGuid(),
                Title = $"Custom Report - {DateTime.UtcNow:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                Format = request.Format,
                Sections = request.Metrics.ToList()
            };

            var reportData = new Dictionary<string, object>();

            foreach (var metric in request.Metrics)
            {
                switch (metric.ToLower())
                {
                    case "sessions":
                        var sessionData = await GetSessionAnalyticsAsync(request.OrganizationId, request.StartDate, request.EndDate);
                        reportData["Sessions"] = sessionData;
                        break;
                        
                    case "performance":
                        var perfData = await GetPerformanceMetricsAsync(request.OrganizationId);
                        reportData["Performance"] = perfData;
                        break;
                        
                    case "users":
                        var userData = await GetUserBehaviorAnalyticsAsync(request.OrganizationId);
                        reportData["Users"] = userData;
                        break;
                        
                    case "business":
                        var businessData = await GetBusinessMetricsAsync(request.OrganizationId);
                        reportData["Business"] = businessData;
                        break;
                }
            }

            report.Metadata = reportData;

            // Generate report in requested format
            switch (request.Format)
            {
                case ReportFormat.Json:
                    report.Data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true }));
                    break;
                    
                case ReportFormat.Csv:
                    report.Data = GenerateCsvReport(reportData);
                    break;
                    
                case ReportFormat.Pdf:
                    report.Data = GeneratePdfReport(reportData);
                    break;
                    
                default:
                    report.Data = Encoding.UTF8.GetBytes("Report generation not implemented for this format");
                    break;
            }

            return report;
        }

        #endregion

        #region Data Export

        public async Task<DataExport> ExportAnalyticsDataAsync(RemoteC.Shared.Models.DataExportRequest request)
        {
            var exportEntity = new RemoteC.Data.Entities.DataExportRequest
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                RequestedBy = Guid.Empty, // Should be set from current user context
                Status = "Processing",
                DataTypes = JsonSerializer.Serialize(request.DataTypes),
                Format = request.Format.ToString(),
                StartDate = request.DateRange.StartDate,
                EndDate = request.DateRange.EndDate,
                Filters = request.Filters.Any() ? JsonSerializer.Serialize(request.Filters) : null,
                CompressOutput = request.CompressOutput,
                CreatedAt = DateTime.UtcNow
            };

            _context.DataExportRequests.Add(exportEntity);
            await _context.SaveChangesAsync();

            // Process export asynchronously (in production, this would be a background job)
            _ = Task.Run(async () => await ProcessExportAsync(exportEntity.Id));

            return new DataExport
            {
                Id = exportEntity.Id,
                FileName = $"analytics_export_{exportEntity.Id}.{request.Format.ToString().ToLower()}",
                Format = request.Format,
                CreatedAt = exportEntity.CreatedAt,
                ExpiresAt = exportEntity.CreatedAt.AddDays(7),
                Status = ExportStatus.Processing
            };
        }

        #endregion

        #region Metrics Collection

        public async Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null)
        {
            _metricsCollector.RecordGauge(metricName, value, tags);
            
            // Also store in database for historical analysis
            var metric = new PerformanceMetric
            {
                OrganizationId = Guid.Empty, // Should be set from context
                MetricName = metricName,
                Value = value,
                Timestamp = DateTime.UtcNow
            };

            _context.PerformanceMetrics.Add(metric);
            await _context.SaveChangesAsync();
        }

        public async Task RecordEventAsync(string eventName, Guid organizationId, Dictionary<string, object>? properties = null)
        {
            var businessEvent = new BusinessEvent
            {
                OrganizationId = organizationId,
                EventType = eventName,
                Timestamp = DateTime.UtcNow,
                Properties = properties != null ? JsonSerializer.Serialize(properties) : null
            };

            _context.BusinessEvents.Add(businessEvent);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helper Methods

        private double GetPercentile(List<double> sortedValues, int percentile)
        {
            if (!sortedValues.Any())
                return 0;

            var index = (percentile / 100.0) * (sortedValues.Count - 1);
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);
            var weight = index - lower;

            if (upper >= sortedValues.Count)
                return sortedValues[lower];

            return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
        }

        private Dictionary<int, int> CreateHistogram(List<double> values, int buckets)
        {
            if (!values.Any())
                return new Dictionary<int, int>();

            var min = values.Min();
            var max = values.Max();
            var bucketSize = (max - min) / buckets;
            
            var histogram = new Dictionary<int, int>();
            for (int i = 0; i < buckets; i++)
            {
                histogram[i] = 0;
            }

            foreach (var value in values)
            {
                var bucket = (int)((value - min) / bucketSize);
                if (bucket >= buckets)
                    bucket = buckets - 1;
                histogram[bucket]++;
            }

            return histogram;
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count < 2)
                return 0;

            var avg = values.Average();
            var sum = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sum / (values.Count - 1));
        }

        private double CalculateAnomalyConfidence(double value, double baseline, double stdDev)
        {
            if (stdDev == 0)
                return 0;

            var zScore = Math.Abs((value - baseline) / stdDev);
            // Convert z-score to confidence (simplified)
            return Math.Min(1.0, zScore / 4.0);
        }

        private double CalculateErrorRate(List<PerformanceMetric> metrics)
        {
            var totalRequests = metrics.Sum(m => m.RequestCount ?? 0);
            var totalErrors = metrics.Sum(m => m.ErrorCount ?? 0);
            
            return totalRequests > 0 ? (totalErrors * 100.0) / totalRequests : 0;
        }

        private int CalculateConsecutiveDays(List<DateTime> dates)
        {
            if (!dates.Any())
                return 0;

            var consecutive = 1;
            for (int i = 1; i < dates.Count; i++)
            {
                if ((dates[i - 1] - dates[i]).Days == 1)
                    consecutive++;
                else
                    break;
            }

            return consecutive;
        }

        private string GetRecommendedAction(ChurnRiskLevel riskLevel, List<string> riskFactors)
        {
            return riskLevel switch
            {
                ChurnRiskLevel.Critical => "Immediate intervention required. Consider personal outreach.",
                ChurnRiskLevel.High => "Schedule a check-in call or send personalized re-engagement campaign.",
                ChurnRiskLevel.Medium => "Send targeted email campaign highlighting new features.",
                _ => "Monitor user activity and consider gentle nudge notifications."
            };
        }

        private bool EvaluateAlertCondition(double currentValue, double threshold, string condition)
        {
            return condition switch
            {
                "GreaterThan" => currentValue > threshold,
                "LessThan" => currentValue < threshold,
                "Equals" => Math.Abs(currentValue - threshold) < 0.001,
                "NotEquals" => Math.Abs(currentValue - threshold) >= 0.001,
                _ => false
            };
        }

        private DateTime CalculateNextRunTime(ReportSchedule schedule)
        {
            var now = DateTime.UtcNow;
            return schedule switch
            {
                ReportSchedule.Daily => now.Date.AddDays(1).AddHours(8), // 8 AM UTC
                ReportSchedule.Weekly => now.Date.AddDays((7 - (int)now.DayOfWeek) % 7 + 1).AddHours(8),
                ReportSchedule.Monthly => new DateTime(now.Year, now.Month, 1).AddMonths(1).AddHours(8),
                ReportSchedule.Quarterly => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddMonths(3).AddHours(8),
                _ => now.AddDays(1)
            };
        }

        private byte[] GenerateCsvReport(Dictionary<string, object> data)
        {
            // Simplified CSV generation
            var csv = new StringBuilder();
            csv.AppendLine("Metric,Value");
            
            foreach (var kvp in data)
            {
                if (kvp.Value is IDictionary<string, object> dict)
                {
                    foreach (var item in dict)
                    {
                        csv.AppendLine($"{kvp.Key}.{item.Key},{item.Value}");
                    }
                }
                else
                {
                    csv.AppendLine($"{kvp.Key},{kvp.Value}");
                }
            }
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GeneratePdfReport(Dictionary<string, object> data)
        {
            // Simplified PDF generation - in production, use a proper PDF library
            var content = new StringBuilder();
            content.AppendLine("ANALYTICS REPORT");
            content.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            content.AppendLine(new string('=', 50));
            
            foreach (var section in data)
            {
                content.AppendLine($"\n{section.Key.ToUpper()}");
                content.AppendLine(new string('-', section.Key.Length));
                content.AppendLine(JsonSerializer.Serialize(section.Value, new JsonSerializerOptions { WriteIndented = true }));
            }
            
            return Encoding.UTF8.GetBytes(content.ToString());
        }

        private async Task ProcessExportAsync(Guid exportId)
        {
            var export = await _context.DataExportRequests.FindAsync(exportId);
            if (export == null)
                return;

            try
            {
                // Simulate export processing
                await Task.Delay(5000);
                
                export.Status = "Completed";
                export.CompletedAt = DateTime.UtcNow;
                export.ExpiresAt = DateTime.UtcNow.AddDays(7);
                export.FileSize = 1024 * 1024; // 1MB placeholder
                export.RecordCount = 1000; // Placeholder
                export.DownloadUrl = $"/api/analytics/export/{export.Id}/download";
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing export {ExportId}", exportId);
                export.Status = "Failed";
                export.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}