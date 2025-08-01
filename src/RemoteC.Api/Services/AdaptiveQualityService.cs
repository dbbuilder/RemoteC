using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service for managing adaptive quality based on network conditions
    /// </summary>
    public class AdaptiveQualityService : IAdaptiveQualityService
    {
        private readonly ILogger<AdaptiveQualityService> _logger;
        private readonly ISessionMetricsService _metricsService;
        private readonly Dictionary<Guid, DateTime> _lastQualityChange = new();
        private readonly Dictionary<Guid, QualitySettings> _currentSettings = new();
        
        // Quality presets
        private readonly Dictionary<QualityPreset, QualityPresetDefinition> _presets;

        public AdaptiveQualityService(
            ILogger<AdaptiveQualityService> logger,
            ISessionMetricsService metricsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _presets = InitializePresets();
        }

        public async Task<QualitySettings> DetermineOptimalQualityAsync(Guid sessionId)
        {
            try
            {
                var metrics = await _metricsService.GetSessionMetricsAsync(sessionId);
                
                // Calculate network score
                var networkScore = await CalculateNetworkScoreAsync(metrics);
                
                // Determine quality based on bandwidth and network conditions
                var quality = DetermineQualityFromMetrics(metrics, networkScore);
                
                _logger.LogInformation("Determined optimal quality for session {SessionId}: {Resolution}p{Fps} @ {BitRate}bps",
                    sessionId, quality.Resolution, quality.FrameRate, quality.BitRate);
                
                return quality;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining optimal quality for session {SessionId}", sessionId);
                return GetDefaultQuality();
            }
        }

        public async Task<QualitySettings> AdjustQualityDynamicallyAsync(Guid sessionId, QualitySettings currentQuality)
        {
            try
            {
                // Check if we should adjust quality yet
                if (!ShouldAdjustQuality(sessionId))
                {
                    return currentQuality;
                }

                var metrics = await _metricsService.GetSessionMetricsAsync(sessionId);
                var trend = await AnalyzeQualityTrendAsync(sessionId, TimeSpan.FromMinutes(1));
                
                QualitySettings newQuality;
                
                if (trend.Trend == QualityTrend.Degrading && trend.ShouldPreemptivelyReduce)
                {
                    // Downgrade quality
                    newQuality = DowngradeQuality(currentQuality, metrics);
                    _logger.LogWarning("Downgrading quality for session {SessionId} due to degrading network conditions", sessionId);
                }
                else if (trend.Trend == QualityTrend.Improving && trend.ShouldAttemptIncrease)
                {
                    // Try to upgrade quality
                    newQuality = UpgradeQuality(currentQuality, metrics);
                    _logger.LogInformation("Upgrading quality for session {SessionId} due to improving network conditions", sessionId);
                }
                else
                {
                    // Fine-tune current quality
                    newQuality = FineTuneQuality(currentQuality, metrics);
                }
                
                _lastQualityChange[sessionId] = DateTime.UtcNow;
                _currentSettings[sessionId] = newQuality;
                
                return newQuality;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting quality dynamically for session {SessionId}", sessionId);
                return currentQuality;
            }
        }

        public async Task<IEnumerable<QualityPresetDefinition>> GetQualityPresetsAsync()
        {
            return await Task.FromResult(_presets.Values.OrderBy(p => p.MinBandwidthBps));
        }

        public async Task<int> CalculateNetworkScoreAsync(SessionMetrics metrics)
        {
            // Score components (0-100 each)
            var bandwidthScore = CalculateBandwidthScore(metrics.BandwidthBps);
            var latencyScore = CalculateLatencyScore(metrics.LatencyMs);
            var packetLossScore = CalculatePacketLossScore(metrics.PacketLoss);
            var jitterScore = CalculateJitterScore(metrics.Jitter);
            
            // Weighted average
            var score = (int)(
                bandwidthScore * 0.4 +
                latencyScore * 0.3 +
                packetLossScore * 0.2 +
                jitterScore * 0.1
            );
            
            return await Task.FromResult(Math.Max(0, Math.Min(100, score)));
        }

        public async Task<QualitySettings> SuggestQualitySettingsAsync(DeviceCapabilities device, SessionMetrics metrics)
        {
            var baseQuality = DetermineQualityFromMetrics(metrics, await CalculateNetworkScoreAsync(metrics));
            
            // Adjust for device capabilities
            if (baseQuality.Resolution > device.MaxResolution)
            {
                baseQuality.Resolution = device.MaxResolution;
            }
            
            if (baseQuality.FrameRate > device.MaxFrameRate)
            {
                baseQuality.FrameRate = device.MaxFrameRate;
            }
            
            // Choose best available encoder
            if (device.AvailableEncoders.Contains("h265") && metrics.BandwidthBps < 3_000_000)
            {
                baseQuality.Encoder = "h265"; // Better compression for low bandwidth
            }
            else if (device.AvailableEncoders.Contains("h264"))
            {
                baseQuality.Encoder = "h264";
            }
            
            // Use hardware encoding if available and beneficial
            baseQuality.UseHardwareEncoding = device.SupportsHardwareEncoding && 
                                              (baseQuality.Resolution >= 1080 || baseQuality.FrameRate >= 30);
            
            // Adjust based on CPU cores
            if (device.CpuCores < 4)
            {
                baseQuality.EnableBFrames = false; // Reduce CPU load
                baseQuality.KeyFrameInterval = 30; // More frequent keyframes
            }
            
            return baseQuality;
        }

        public async Task<QualityTrendAnalysis> AnalyzeQualityTrendAsync(Guid sessionId, TimeSpan window)
        {
            var historicalMetrics = await _metricsService.GetHistoricalMetricsAsync(sessionId, window);
            
            if (!historicalMetrics.Any())
            {
                return new QualityTrendAnalysis
                {
                    Trend = QualityTrend.Stable,
                    TrendStrength = 0,
                    StableDuration = TimeSpan.Zero
                };
            }
            
            // Analyze bandwidth trend
            var bandwidthValues = historicalMetrics.Select(m => m.BandwidthBps).ToList();
            var bandwidthTrend = CalculateTrend(bandwidthValues);
            
            // Analyze packet loss trend
            var packetLossValues = historicalMetrics.Select(m => (double)m.PacketLoss).ToList();
            var packetLossTrend = CalculateTrend(packetLossValues);
            
            // Determine overall trend
            QualityTrend overallTrend;
            if (Math.Abs(bandwidthTrend) < 0.1 && Math.Abs(packetLossTrend) < 0.01)
            {
                overallTrend = QualityTrend.Stable;
            }
            else if (bandwidthTrend < -0.2 || packetLossTrend > 0.02)
            {
                overallTrend = QualityTrend.Degrading;
            }
            else if (bandwidthTrend > 0.2 && packetLossTrend < -0.01)
            {
                overallTrend = QualityTrend.Improving;
            }
            else
            {
                overallTrend = QualityTrend.Fluctuating;
            }
            
            // Calculate trend strength
            var trendStrength = Math.Abs(bandwidthTrend) + Math.Abs(packetLossTrend * 10);
            
            return new QualityTrendAnalysis
            {
                Trend = overallTrend,
                TrendStrength = trendStrength,
                ShouldPreemptivelyReduce = overallTrend == QualityTrend.Degrading && trendStrength > 0.5,
                ShouldAttemptIncrease = overallTrend == QualityTrend.Improving && trendStrength > 0.3,
                StableDuration = CalculateStableDuration(historicalMetrics.ToList()),
                Metrics = new Dictionary<string, double>
                {
                    ["BandwidthTrend"] = bandwidthTrend,
                    ["PacketLossTrend"] = packetLossTrend,
                    ["AverageBandwidth"] = bandwidthValues.Average(),
                    ["AveragePacketLoss"] = packetLossValues.Average()
                }
            };
        }

        public async Task<QualityAdjustmentResult> ApplyQualitySettingsAsync(Guid sessionId, QualitySettings settings)
        {
            try
            {
                // Validate settings
                if (settings.Resolution <= 0 || settings.FrameRate <= 0 || settings.BitRate <= 0)
                {
                    return new QualityAdjustmentResult
                    {
                        Success = false,
                        Reason = "Invalid quality settings"
                    };
                }
                
                // Store current settings
                _currentSettings[sessionId] = settings;
                _lastQualityChange[sessionId] = DateTime.UtcNow;
                
                _logger.LogInformation("Applied quality settings for session {SessionId}: {Resolution}p{Fps} @ {BitRate}bps",
                    sessionId, settings.Resolution, settings.FrameRate, settings.BitRate);
                
                return new QualityAdjustmentResult
                {
                    Success = true,
                    AppliedSettings = settings,
                    Reason = "Quality settings applied successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying quality settings for session {SessionId}", sessionId);
                return new QualityAdjustmentResult
                {
                    Success = false,
                    Reason = $"Error: {ex.Message}"
                };
            }
        }

        private Dictionary<QualityPreset, QualityPresetDefinition> InitializePresets()
        {
            return new Dictionary<QualityPreset, QualityPresetDefinition>
            {
                [QualityPreset.VeryLow] = new QualityPresetDefinition
                {
                    Name = "Very Low",
                    Description = "For very poor connections",
                    MinBandwidthBps = 200_000,
                    RecommendedBandwidthBps = 500_000,
                    Settings = new QualitySettings
                    {
                        Resolution = 480,
                        FrameRate = 10,
                        BitRate = 300_000,
                        Preset = QualityPreset.VeryLow,
                        EncodingProfile = EncodingProfile.LowLatency,
                        KeyFrameInterval = 20,
                        EnableBFrames = false,
                        JpegQuality = 60,
                        ColorDepth = 16
                    }
                },
                [QualityPreset.Low] = new QualityPresetDefinition
                {
                    Name = "Low",
                    Description = "For slow connections",
                    MinBandwidthBps = 500_000,
                    RecommendedBandwidthBps = 1_000_000,
                    Settings = new QualitySettings
                    {
                        Resolution = 720,
                        FrameRate = 15,
                        BitRate = 750_000,
                        Preset = QualityPreset.Low,
                        EncodingProfile = EncodingProfile.Balanced,
                        KeyFrameInterval = 30,
                        EnableBFrames = false,
                        JpegQuality = 70,
                        ColorDepth = 24
                    }
                },
                [QualityPreset.Medium] = new QualityPresetDefinition
                {
                    Name = "Medium",
                    Description = "Balanced quality and performance",
                    MinBandwidthBps = 1_500_000,
                    RecommendedBandwidthBps = 3_000_000,
                    Settings = new QualitySettings
                    {
                        Resolution = 1080,
                        FrameRate = 30,
                        BitRate = 2_500_000,
                        Preset = QualityPreset.Medium,
                        EncodingProfile = EncodingProfile.Balanced,
                        KeyFrameInterval = 60,
                        EnableBFrames = true,
                        JpegQuality = 80,
                        ColorDepth = 24
                    }
                },
                [QualityPreset.High] = new QualityPresetDefinition
                {
                    Name = "High",
                    Description = "For good connections",
                    MinBandwidthBps = 5_000_000,
                    RecommendedBandwidthBps = 8_000_000,
                    Settings = new QualitySettings
                    {
                        Resolution = 1080,
                        FrameRate = 30,
                        BitRate = 6_000_000,
                        Preset = QualityPreset.High,
                        EncodingProfile = EncodingProfile.HighQuality,
                        KeyFrameInterval = 90,
                        EnableBFrames = true,
                        JpegQuality = 90,
                        ColorDepth = 24
                    }
                },
                [QualityPreset.VeryHigh] = new QualityPresetDefinition
                {
                    Name = "Very High",
                    Description = "Maximum quality",
                    MinBandwidthBps = 10_000_000,
                    RecommendedBandwidthBps = 20_000_000,
                    Settings = new QualitySettings
                    {
                        Resolution = 1080,
                        FrameRate = 60,
                        BitRate = 15_000_000,
                        Preset = QualityPreset.VeryHigh,
                        EncodingProfile = EncodingProfile.HighQuality,
                        KeyFrameInterval = 120,
                        EnableBFrames = true,
                        JpegQuality = 95,
                        ColorDepth = 32
                    }
                }
            };
        }

        private QualitySettings DetermineQualityFromMetrics(SessionMetrics metrics, int networkScore)
        {
            // Find best matching preset based on bandwidth
            var preset = _presets.Values
                .Where(p => metrics.BandwidthBps >= p.MinBandwidthBps)
                .OrderByDescending(p => p.MinBandwidthBps)
                .FirstOrDefault();
            
            if (preset == null)
            {
                preset = _presets[QualityPreset.VeryLow];
            }
            
            var quality = preset.Settings.Clone();
            
            // Adjust for network conditions
            if (metrics.PacketLoss > 0.03f) // > 3% packet loss
            {
                quality.EnableFEC = true;
                quality.KeyFrameInterval = Math.Min(quality.KeyFrameInterval, 30);
            }
            
            if (metrics.LatencyMs > 100) // High latency
            {
                quality.EncodingProfile = EncodingProfile.LowLatency;
                quality.EnableBFrames = false;
            }
            
            // Adjust bitrate to fit available bandwidth with margin
            quality.BitRate = (long)(metrics.BandwidthBps * 0.8);
            
            return quality;
        }

        private bool ShouldAdjustQuality(Guid sessionId)
        {
            if (!_lastQualityChange.TryGetValue(sessionId, out var lastChange))
            {
                return true;
            }
            
            var timeSinceLastChange = DateTime.UtcNow - lastChange;
            return timeSinceLastChange > TimeSpan.FromSeconds(5);
        }

        private QualitySettings DowngradeQuality(QualitySettings current, SessionMetrics metrics)
        {
            var newQuality = current.Clone();
            
            // Reduce resolution or framerate
            if (current.Resolution > 720)
            {
                newQuality.Resolution = 720;
            }
            else if (current.FrameRate > 15)
            {
                newQuality.FrameRate = Math.Max(10, current.FrameRate - 10);
            }
            else
            {
                newQuality.Resolution = Math.Max(480, current.Resolution - 240);
            }
            
            // Reduce bitrate
            newQuality.BitRate = (long)(metrics.BandwidthBps * 0.7);
            
            // Adjust other settings for lower quality
            newQuality.JpegQuality = Math.Max(60, current.JpegQuality - 10);
            newQuality.EnableBFrames = false;
            newQuality.KeyFrameInterval = Math.Min(30, current.KeyFrameInterval);
            
            return newQuality;
        }

        private QualitySettings UpgradeQuality(QualitySettings current, SessionMetrics metrics)
        {
            var newQuality = current.Clone();
            
            // Check if we can increase resolution or framerate
            var availableBandwidth = metrics.BandwidthBps * 0.8;
            var estimatedBitrate = EstimateBitrate(current.Resolution, current.FrameRate);
            
            if (availableBandwidth > estimatedBitrate * 1.5)
            {
                if (current.Resolution < 1080)
                {
                    newQuality.Resolution = 1080;
                }
                else if (current.FrameRate < 60)
                {
                    newQuality.FrameRate = Math.Min(60, current.FrameRate + 15);
                }
            }
            
            // Increase bitrate
            newQuality.BitRate = Math.Min((long)availableBandwidth, estimatedBitrate);
            
            // Improve other settings
            newQuality.JpegQuality = Math.Min(95, current.JpegQuality + 5);
            newQuality.EnableBFrames = true;
            
            return newQuality;
        }

        private QualitySettings FineTuneQuality(QualitySettings current, SessionMetrics metrics)
        {
            var newQuality = current.Clone();
            
            // Fine-tune bitrate based on current conditions
            var targetBitrate = (long)(metrics.BandwidthBps * 0.8);
            newQuality.BitRate = (long)(current.BitRate * 0.9 + targetBitrate * 0.1); // Smooth adjustment
            
            // Adjust encoding parameters based on CPU usage
            if (metrics.CpuUsage > 80)
            {
                newQuality.UseHardwareEncoding = true;
                newQuality.EnableBFrames = false;
            }
            
            return newQuality;
        }

        private int CalculateBandwidthScore(long bandwidthBps)
        {
            if (bandwidthBps >= 10_000_000) return 100; // 10 Mbps+
            if (bandwidthBps >= 5_000_000) return 90;   // 5 Mbps
            if (bandwidthBps >= 3_000_000) return 75;   // 3 Mbps
            if (bandwidthBps >= 1_500_000) return 60;   // 1.5 Mbps
            if (bandwidthBps >= 1_000_000) return 45;   // 1 Mbps
            if (bandwidthBps >= 500_000) return 30;     // 500 Kbps
            return 15; // < 500 Kbps
        }

        private int CalculateLatencyScore(double latencyMs)
        {
            if (latencyMs <= 10) return 100;
            if (latencyMs <= 20) return 95;
            if (latencyMs <= 50) return 85;
            if (latencyMs <= 100) return 70;
            if (latencyMs <= 150) return 50;
            if (latencyMs <= 200) return 30;
            return 10;
        }

        private int CalculatePacketLossScore(float packetLoss)
        {
            if (packetLoss <= 0.001f) return 100; // 0.1%
            if (packetLoss <= 0.005f) return 90;  // 0.5%
            if (packetLoss <= 0.01f) return 75;   // 1%
            if (packetLoss <= 0.02f) return 60;   // 2%
            if (packetLoss <= 0.05f) return 40;   // 5%
            return 20;
        }

        private int CalculateJitterScore(float jitter)
        {
            if (jitter <= 1) return 100;
            if (jitter <= 5) return 90;
            if (jitter <= 10) return 75;
            if (jitter <= 20) return 60;
            if (jitter <= 50) return 40;
            return 20;
        }

        private double CalculateTrend(List<long> values)
        {
            if (values.Count < 2) return 0;
            
            // Simple linear regression
            var n = values.Count;
            var sumX = 0.0;
            var sumY = 0.0;
            var sumXY = 0.0;
            var sumX2 = 0.0;
            
            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += values[i];
                sumXY += i * values[i];
                sumX2 += i * i;
            }
            
            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var mean = sumY / n;
            
            return mean > 0 ? slope / mean : 0; // Normalized slope
        }

        private double CalculateTrend(List<double> values)
        {
            return CalculateTrend(values.Select(v => (long)(v * 1000)).ToList()) / 1000.0;
        }

        private TimeSpan CalculateStableDuration(List<SessionMetrics> metrics)
        {
            if (metrics.Count < 2) return TimeSpan.Zero;
            
            var stableThreshold = 0.1; // 10% variation
            var lastStableTime = metrics.First().Timestamp;
            var referenceBandwidth = metrics.First().BandwidthBps;
            
            foreach (var metric in metrics.Skip(1))
            {
                var variation = Math.Abs(metric.BandwidthBps - referenceBandwidth) / (double)referenceBandwidth;
                if (variation > stableThreshold)
                {
                    lastStableTime = metric.Timestamp;
                    referenceBandwidth = metric.BandwidthBps;
                }
            }
            
            return DateTime.UtcNow - lastStableTime;
        }

        private long EstimateBitrate(int resolution, int frameRate)
        {
            // Rough estimation based on resolution and framerate
            var pixels = resolution switch
            {
                480 => 640 * 480,
                720 => 1280 * 720,
                1080 => 1920 * 1080,
                1440 => 2560 * 1440,
                2160 => 3840 * 2160,
                _ => resolution * resolution * 16 / 9
            };
            
            // Base bitrate calculation
            var baseBitrate = pixels * frameRate * 0.1; // 0.1 bits per pixel
            
            return (long)baseBitrate;
        }

        private QualitySettings GetDefaultQuality()
        {
            return _presets[QualityPreset.Medium].Settings.Clone();
        }

        public async Task UpdateQualitySettingsAsync(Guid sessionId, QualitySettings settings)
        {
            _currentSettings[sessionId] = settings;
            _lastQualityChange[sessionId] = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        public async Task<QualitySettings> GetCurrentQualityAsync(Guid sessionId)
        {
            if (_currentSettings.TryGetValue(sessionId, out var settings))
            {
                return await Task.FromResult(settings);
            }
            return await Task.FromResult(GetDefaultQuality());
        }

        public async Task ReportMetricsAsync(Guid sessionId, RemoteC.Shared.Models.SessionMetrics metrics)
        {
            // This would typically record the metrics for analysis
            await _metricsService.RecordSessionMetricsAsync(sessionId, metrics);
        }

        public QualitySettings GetQualityPreset(RemoteC.Shared.Models.QualityLevel level)
        {
            var preset = level switch
            {
                RemoteC.Shared.Models.QualityLevel.VeryLow => QualityPreset.VeryLow,
                RemoteC.Shared.Models.QualityLevel.Low => QualityPreset.Low,
                RemoteC.Shared.Models.QualityLevel.Medium => QualityPreset.Medium,
                RemoteC.Shared.Models.QualityLevel.High => QualityPreset.High,
                RemoteC.Shared.Models.QualityLevel.VeryHigh => QualityPreset.VeryHigh,
                _ => QualityPreset.Medium
            };
            
            return _presets[preset].Settings.Clone();
        }

        public async Task<bool> ShouldDowngradeQualityAsync(Guid sessionId)
        {
            var metrics = await _metricsService.GetSessionMetricsAsync(sessionId);
            var networkScore = await CalculateNetworkScoreAsync(metrics);
            return ShouldDowngradeQuality(metrics, networkScore);
        }

        public async Task<bool> ShouldUpgradeQualityAsync(Guid sessionId)
        {
            var metrics = await _metricsService.GetSessionMetricsAsync(sessionId);
            var networkScore = await CalculateNetworkScoreAsync(metrics);
            return ShouldUpgradeQuality(metrics, networkScore);
        }

        private bool ShouldDowngradeQuality(SessionMetrics metrics, int networkScore)
        {
            return networkScore < 50 || metrics.PacketLoss > 0.05f || metrics.CpuUsage > 90;
        }

        private bool ShouldUpgradeQuality(SessionMetrics metrics, int networkScore)
        {
            return networkScore > 80 && metrics.PacketLoss < 0.01f && metrics.CpuUsage < 60;
        }
    }

    /// <summary>
    /// Extension methods for QualitySettings
    /// </summary>
    public static class QualitySettingsExtensions
    {
        public static QualitySettings Clone(this QualitySettings settings)
        {
            return new QualitySettings
            {
                Resolution = settings.Resolution,
                FrameRate = settings.FrameRate,
                BitRate = settings.BitRate,
                Encoder = settings.Encoder,
                Preset = settings.Preset,
                EncodingProfile = settings.EncodingProfile,
                KeyFrameInterval = settings.KeyFrameInterval,
                EnableBFrames = settings.EnableBFrames,
                UseHardwareEncoding = settings.UseHardwareEncoding,
                EnableFEC = settings.EnableFEC,
                JpegQuality = settings.JpegQuality,
                EnableAdaptiveBitrate = settings.EnableAdaptiveBitrate,
                ColorDepth = settings.ColorDepth,
                EnableFrameSkipping = settings.EnableFrameSkipping
            };
        }
    }
}