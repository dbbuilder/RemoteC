using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class MetricsService : IMetricsService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly Dictionary<string, List<MetricDataPoint>> _metrics = new();
        private readonly Dictionary<Guid, DeploymentMetrics> _deploymentMetrics = new();
        private readonly List<Alert> _alerts = new();
        private readonly List<AlertRule> _alertRules = new();

        public MetricsService(ILogger<MetricsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<DeploymentMetrics> GetDeploymentMetricsAsync(Guid deploymentId)
        {
            if (_deploymentMetrics.TryGetValue(deploymentId, out var metrics))
            {
                return Task.FromResult(metrics);
            }

            // Generate random metrics for demonstration
            var deploymentMetrics = new DeploymentMetrics
            {
                CPUUsagePercent = Random.Shared.Next(10, 95),
                MemoryUsagePercent = Random.Shared.Next(20, 90),
                NetworkLatency = Random.Shared.Next(1, 200),
                ErrorRate = Random.Shared.NextDouble() * 0.05, // 0-5% error rate
                CacheMissRate = Random.Shared.NextDouble() * 0.7, // 0-70% cache miss rate
                AverageResponseTime = Random.Shared.Next(10, 500),
                RequestsPerSecond = Random.Shared.Next(10, 1000)
            };

            _deploymentMetrics[deploymentId] = deploymentMetrics;
            return Task.FromResult(deploymentMetrics);
        }

        public Task<NodeMetrics> GetNodeMetricsAsync(Guid nodeId)
        {
            // Generate random metrics for demonstration
            return Task.FromResult(new NodeMetrics
            {
                NodeId = nodeId,
                CPUUsage = Random.Shared.Next(10, 80),
                MemoryUsage = Random.Shared.Next(20, 70),
                DiskUsage = Random.Shared.Next(10, 60),
                NetworkIn = Random.Shared.Next(1000, 1000000),
                NetworkOut = Random.Shared.Next(1000, 1000000),
                RunningContainers = Random.Shared.Next(1, 20),
                CollectedAt = DateTime.UtcNow
            });
        }

        public Task<List<MetricDataPoint>> GetMetricTimeSeriesAsync(string metricName, DateTime start, DateTime end)
        {
            if (!_metrics.TryGetValue(metricName, out var dataPoints))
            {
                dataPoints = GenerateTimeSeriesData(metricName, start, end);
                _metrics[metricName] = dataPoints;
            }

            var filtered = dataPoints
                .Where(dp => dp.Timestamp >= start && dp.Timestamp <= end)
                .OrderBy(dp => dp.Timestamp)
                .ToList();

            return Task.FromResult(filtered);
        }

        public Task RecordMetricAsync(string metricName, double value, Dictionary<string, string> tags)
        {
            if (!_metrics.ContainsKey(metricName))
            {
                _metrics[metricName] = new List<MetricDataPoint>();
            }

            var dataPoint = new MetricDataPoint
            {
                Timestamp = DateTime.UtcNow,
                Value = value,
                Tags = tags
            };

            _metrics[metricName].Add(dataPoint);

            // Check alert rules
            CheckAlertRules(metricName, value, tags);

            return Task.CompletedTask;
        }

        public Task<AggregatedMetrics> GetAggregatedMetricsAsync(string metricName, TimeSpan window, AggregationType type)
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime - window;

            var dataPoints = GetMetricTimeSeriesAsync(metricName, startTime, endTime).Result;
            
            if (!dataPoints.Any())
            {
                return Task.FromResult(new AggregatedMetrics
                {
                    Min = 0,
                    Max = 0,
                    Average = 0,
                    Sum = 0,
                    Count = 0,
                    StandardDeviation = 0
                });
            }

            var values = dataPoints.Select(dp => dp.Value).ToList();
            
            var aggregated = new AggregatedMetrics
            {
                Min = values.Min(),
                Max = values.Max(),
                Average = values.Average(),
                Sum = values.Sum(),
                Count = values.Count,
                StandardDeviation = CalculateStandardDeviation(values)
            };

            return Task.FromResult(aggregated);
        }

        public Task<List<Alert>> GetActiveAlertsAsync(string? deploymentId = null)
        {
            var activeAlerts = _alerts
                .Where(a => a.TriggeredAt > DateTime.UtcNow.AddHours(-24)) // Last 24 hours
                .ToList();

            if (!string.IsNullOrEmpty(deploymentId))
            {
                activeAlerts = activeAlerts
                    .Where(a => a.Tags.ContainsKey("deploymentId") && a.Tags["deploymentId"] == deploymentId)
                    .ToList();
            }

            return Task.FromResult(activeAlerts);
        }

        public Task<bool> CreateAlertRuleAsync(AlertRule rule)
        {
            _alertRules.Add(rule);
            _logger.LogInformation("Created alert rule {RuleName} for metric {MetricName}", 
                rule.Name, rule.MetricName);
            return Task.FromResult(true);
        }

        private List<MetricDataPoint> GenerateTimeSeriesData(string metricName, DateTime start, DateTime end)
        {
            var dataPoints = new List<MetricDataPoint>();
            var currentTime = start;
            var interval = TimeSpan.FromMinutes(5);
            var baseValue = Random.Shared.Next(20, 80);

            while (currentTime <= end)
            {
                var value = baseValue + Random.Shared.Next(-10, 10);
                value = Math.Max(0, Math.Min(100, value)); // Keep between 0-100

                dataPoints.Add(new MetricDataPoint
                {
                    Timestamp = currentTime,
                    Value = value,
                    Tags = new Dictionary<string, string>
                    {
                        ["metric"] = metricName
                    }
                });

                currentTime = currentTime.Add(interval);
            }

            return dataPoints;
        }

        private double CalculateStandardDeviation(List<double> values)
        {
            if (values.Count <= 1) return 0;

            var average = values.Average();
            var sumOfSquaresOfDifferences = values.Sum(val => (val - average) * (val - average));
            return Math.Sqrt(sumOfSquaresOfDifferences / (values.Count - 1));
        }

        private void CheckAlertRules(string metricName, double value, Dictionary<string, string> tags)
        {
            var applicableRules = _alertRules.Where(r => r.MetricName == metricName);

            foreach (var rule in applicableRules)
            {
                bool shouldAlert = false;

                switch (rule.Condition)
                {
                    case AlertCondition.GreaterThan:
                        shouldAlert = value > rule.Threshold;
                        break;
                    case AlertCondition.LessThan:
                        shouldAlert = value < rule.Threshold;
                        break;
                    case AlertCondition.GreaterThanOrEqual:
                        shouldAlert = value >= rule.Threshold;
                        break;
                    case AlertCondition.LessThanOrEqual:
                        shouldAlert = value <= rule.Threshold;
                        break;
                    case AlertCondition.Equal:
                        shouldAlert = Math.Abs(value - rule.Threshold) < 0.001;
                        break;
                    case AlertCondition.NotEqual:
                        shouldAlert = Math.Abs(value - rule.Threshold) >= 0.001;
                        break;
                }

                if (shouldAlert)
                {
                    var alert = new Alert
                    {
                        Id = Guid.NewGuid(),
                        Name = rule.Name,
                        Severity = rule.Severity,
                        Message = $"Metric {metricName} value {value} triggered rule {rule.Name}",
                        TriggeredAt = DateTime.UtcNow,
                        Tags = new Dictionary<string, string>(tags)
                    };

                    _alerts.Add(alert);
                    _logger.LogWarning("Alert triggered: {AlertName} - {Message}", alert.Name, alert.Message);
                }
            }
        }
    }
}