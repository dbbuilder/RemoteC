using System.Collections.Concurrent;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services;

/// <summary>
/// Service for tracking and managing session performance metrics
/// </summary>
public class SessionMetricsService : ISessionMetricsService
{
    private readonly ConcurrentDictionary<Guid, List<SessionMetrics>> _metricsStore = new();
    private readonly ILogger<SessionMetricsService> _logger;

    public SessionMetricsService(ILogger<SessionMetricsService> logger)
    {
        _logger = logger;
    }

    public Task RecordSessionMetricsAsync(Guid sessionId, SessionMetrics metrics)
    {
        _metricsStore.AddOrUpdate(sessionId, 
            new List<SessionMetrics> { metrics },
            (key, list) => 
            {
                list.Add(metrics);
                // Keep only last 1000 metrics per session
                if (list.Count > 1000)
                {
                    list.RemoveAt(0);
                }
                return list;
            });

        _logger.LogDebug("Recorded metrics for session {SessionId}: Bandwidth={Bandwidth}bps, Latency={Latency}ms", 
            sessionId, metrics.BandwidthBps, metrics.LatencyMs);

        return Task.CompletedTask;
    }

    public Task<SessionMetrics?> GetSessionMetricsAsync(Guid sessionId)
    {
        if (_metricsStore.TryGetValue(sessionId, out var metricsList) && metricsList.Any())
        {
            return Task.FromResult<SessionMetrics?>(metricsList.Last());
        }

        return Task.FromResult<SessionMetrics?>(null);
    }

    public Task<IEnumerable<SessionMetrics>> GetHistoricalMetricsAsync(Guid sessionId, TimeSpan duration)
    {
        if (_metricsStore.TryGetValue(sessionId, out var metricsList))
        {
            var cutoffTime = DateTime.UtcNow.Subtract(duration);
            var historicalMetrics = metricsList
                .Where(m => m.Timestamp >= cutoffTime)
                .ToList();

            return Task.FromResult<IEnumerable<SessionMetrics>>(historicalMetrics);
        }

        return Task.FromResult<IEnumerable<SessionMetrics>>(Enumerable.Empty<SessionMetrics>());
    }

    public Task ClearMetricsAsync(Guid sessionId)
    {
        _metricsStore.TryRemove(sessionId, out _);
        _logger.LogDebug("Cleared metrics for session {SessionId}", sessionId);
        return Task.CompletedTask;
    }
}