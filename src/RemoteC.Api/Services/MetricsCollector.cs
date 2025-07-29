using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RemoteC.Api.Services
{
    public class MetricsCollector : IMetricsCollector
    {
        private readonly ILogger<MetricsCollector> _logger;
        private readonly ConcurrentDictionary<string, long> _counters = new();
        private readonly ConcurrentDictionary<string, List<double>> _histograms = new();
        private readonly ConcurrentDictionary<string, double> _gauges = new();
        private readonly ConcurrentDictionary<string, Stopwatch> _timers = new();

        public MetricsCollector(ILogger<MetricsCollector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RecordCounter(string name, double value = 1, Dictionary<string, string>? tags = null)
        {
            var key = BuildKey(name, tags);
            _counters.AddOrUpdate(key, (long)value, (_, oldValue) => oldValue + (long)value);
            _logger.LogDebug("Recorded counter {Name} by {Value}", name, value);
        }

        public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
        {
            var key = BuildKey(name, tags);
            _gauges[key] = value;
            _logger.LogDebug("Recorded gauge {Name} = {Value}", name, value);
        }

        public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
        {
            var key = BuildKey(name, tags);
            _histograms.AddOrUpdate(key, 
                new List<double> { value }, 
                (_, list) => 
                {
                    list.Add(value);
                    return list;
                });
            _logger.LogDebug("Recorded histogram {Name} value {Value}", name, value);
        }

        public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
        {
            var key = BuildKey(name, tags);
            var stopwatch = Stopwatch.StartNew();
            _timers[key] = stopwatch;
            
            return new TimerScope(this, key, stopwatch);
        }

        public void RecordTimer(string name, double milliseconds, Dictionary<string, string>? tags = null)
        {
            RecordHistogram(name, milliseconds, tags);
            _logger.LogDebug("Recorded timer {Name} = {Value}ms", name, milliseconds);
        }

        public double GetGaugeValue(string name, Dictionary<string, string>? tags = null)
        {
            var key = BuildKey(name, tags);
            return _gauges.TryGetValue(key, out var value) ? value : 0;
        }

        public long GetCounterValue(string name, Dictionary<string, string>? tags = null)
        {
            var key = BuildKey(name, tags);
            return _counters.TryGetValue(key, out var value) ? value : 0;
        }

        public async Task<Dictionary<string, object>> GetMetricsAsync()
        {
            var metrics = new Dictionary<string, object>
            {
                ["counters"] = new Dictionary<string, long>(_counters),
                ["gauges"] = new Dictionary<string, double>(_gauges),
                ["histograms"] = ComputeHistogramStats(),
                ["timestamp"] = DateTime.UtcNow
            };

            return await Task.FromResult(metrics);
        }

        public async Task FlushAsync()
        {
            _logger.LogInformation("Flushing metrics - Counters: {CounterCount}, Gauges: {GaugeCount}, Histograms: {HistogramCount}",
                _counters.Count, _gauges.Count, _histograms.Count);
            
            // In a real implementation, this would send metrics to a time-series database
            _counters.Clear();
            _histograms.Clear();
            
            await Task.CompletedTask;
        }

        private string BuildKey(string name, Dictionary<string, string>? tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return name;
            }

            var tagString = string.Join(",", tags);
            return $"{name}:{tagString}";
        }

        private Dictionary<string, object> ComputeHistogramStats()
        {
            var stats = new Dictionary<string, object>();
            
            foreach (var kvp in _histograms)
            {
                var values = kvp.Value;
                if (values.Count == 0) continue;

                values.Sort();
                
                stats[kvp.Key] = new
                {
                    count = values.Count,
                    min = values[0],
                    max = values[values.Count - 1],
                    mean = CalculateMean(values),
                    p50 = CalculatePercentile(values, 0.5),
                    p95 = CalculatePercentile(values, 0.95),
                    p99 = CalculatePercentile(values, 0.99)
                };
            }

            return stats;
        }

        private double CalculateMean(List<double> values)
        {
            if (values.Count == 0) return 0;
            
            double sum = 0;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }

        private double CalculatePercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;
            
            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
            
            return sortedValues[index];
        }

        private class TimerScope : IDisposable
        {
            private readonly MetricsCollector _collector;
            private readonly string _key;
            private readonly Stopwatch _stopwatch;
            private bool _disposed;

            public TimerScope(MetricsCollector collector, string key, Stopwatch stopwatch)
            {
                _collector = collector;
                _key = key;
                _stopwatch = stopwatch;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _stopwatch.Stop();
                    _collector.RecordHistogram(_key, _stopwatch.ElapsedMilliseconds);
                    _collector._timers.TryRemove(_key, out _);
                    _disposed = true;
                }
            }
        }
    }
}