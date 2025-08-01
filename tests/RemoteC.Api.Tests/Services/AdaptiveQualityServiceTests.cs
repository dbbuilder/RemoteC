using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class AdaptiveQualityServiceTests
    {
        private readonly Mock<ILogger<AdaptiveQualityService>> _mockLogger;
        private readonly Mock<ISessionMetricsService> _mockMetricsService;
        private readonly AdaptiveQualityService _service;

        public AdaptiveQualityServiceTests()
        {
            _mockLogger = new Mock<ILogger<AdaptiveQualityService>>();
            _mockMetricsService = new Mock<ISessionMetricsService>();
            _service = new AdaptiveQualityService(_mockLogger.Object, _mockMetricsService.Object);
        }

        [Fact]
        public async Task DetermineOptimalQuality_LowBandwidth_ShouldReturnLowQuality()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var metrics = new SessionMetrics
            {
                BandwidthBps = 500_000, // 500 Kbps
                LatencyMs = 50,
                PacketLoss = 0.01f,
                CpuUsage = 30,
                MemoryUsageMb = 512
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(metrics);

            // Act
            var quality = await _service.DetermineOptimalQualityAsync(sessionId);

            // Assert
            Assert.NotNull(quality);
            Assert.True(quality.Resolution <= 720); // Should use lower resolution
            Assert.True(quality.FrameRate <= 15); // Should use lower frame rate
            Assert.True(quality.BitRate <= 500_000); // Should match available bandwidth
            Assert.Equal(QualityPreset.Low, quality.Preset);
        }

        [Fact]
        public async Task DetermineOptimalQuality_HighBandwidth_ShouldReturnHighQuality()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var metrics = new SessionMetrics
            {
                BandwidthBps = 10_000_000, // 10 Mbps
                LatencyMs = 10,
                PacketLoss = 0.001f,
                CpuUsage = 20,
                MemoryUsageMb = 1024
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(metrics);

            // Act
            var quality = await _service.DetermineOptimalQualityAsync(sessionId);

            // Assert
            Assert.NotNull(quality);
            Assert.True(quality.Resolution >= 1080); // Should use higher resolution
            Assert.True(quality.FrameRate >= 30); // Should use higher frame rate
            Assert.True(quality.BitRate >= 5_000_000); // Should use higher bitrate
            Assert.Equal(QualityPreset.High, quality.Preset);
        }

        [Fact]
        public async Task DetermineOptimalQuality_HighPacketLoss_ShouldReduceQuality()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var metrics = new SessionMetrics
            {
                BandwidthBps = 5_000_000, // 5 Mbps
                LatencyMs = 20,
                PacketLoss = 0.05f, // 5% packet loss
                CpuUsage = 30,
                MemoryUsageMb = 1024
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(metrics);

            // Act
            var quality = await _service.DetermineOptimalQualityAsync(sessionId);

            // Assert
            Assert.NotNull(quality);
            Assert.True(quality.KeyFrameInterval <= 30); // More frequent keyframes
            Assert.True(quality.EnableFEC); // Forward Error Correction should be enabled
            Assert.Equal(QualityPreset.Medium, quality.Preset);
        }

        [Fact]
        public async Task DetermineOptimalQuality_HighLatency_ShouldOptimizeForLatency()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var metrics = new SessionMetrics
            {
                BandwidthBps = 3_000_000, // 3 Mbps
                LatencyMs = 150, // High latency
                PacketLoss = 0.01f,
                CpuUsage = 40,
                MemoryUsageMb = 1024
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(metrics);

            // Act
            var quality = await _service.DetermineOptimalQualityAsync(sessionId);

            // Assert
            Assert.NotNull(quality);
            Assert.True(quality.UseHardwareEncoding); // Prefer hardware encoding for lower latency
            Assert.False(quality.EnableBFrames); // Disable B-frames for lower latency
            Assert.Equal(EncodingProfile.LowLatency, quality.EncodingProfile);
        }

        [Fact]
        public async Task AdjustQualityDynamically_BandwidthDrop_ShouldDowngrade()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var initialQuality = new QualitySettings
            {
                Resolution = 1080,
                FrameRate = 30,
                BitRate = 5_000_000,
                Preset = QualityPreset.High
            };

            var newMetrics = new SessionMetrics
            {
                BandwidthBps = 1_000_000, // Bandwidth dropped to 1 Mbps
                LatencyMs = 30,
                PacketLoss = 0.02f,
                CpuUsage = 50,
                MemoryUsageMb = 1024
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(newMetrics);

            // Act
            var adjustedQuality = await _service.AdjustQualityDynamicallyAsync(sessionId, initialQuality);

            // Assert
            Assert.NotNull(adjustedQuality);
            Assert.True(adjustedQuality.Resolution < initialQuality.Resolution);
            Assert.True(adjustedQuality.BitRate < initialQuality.BitRate);
            Assert.NotEqual(QualityPreset.High, adjustedQuality.Preset);
        }

        [Fact]
        public async Task AdjustQualityDynamically_BandwidthIncrease_ShouldUpgrade()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var initialQuality = new QualitySettings
            {
                Resolution = 720,
                FrameRate = 15,
                BitRate = 1_000_000,
                Preset = QualityPreset.Low
            };

            var newMetrics = new SessionMetrics
            {
                BandwidthBps = 8_000_000, // Bandwidth increased to 8 Mbps
                LatencyMs = 15,
                PacketLoss = 0.001f,
                CpuUsage = 25,
                MemoryUsageMb = 2048
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(newMetrics);

            // Act
            var adjustedQuality = await _service.AdjustQualityDynamicallyAsync(sessionId, initialQuality);

            // Assert
            Assert.NotNull(adjustedQuality);
            Assert.True(adjustedQuality.Resolution > initialQuality.Resolution);
            Assert.True(adjustedQuality.FrameRate > initialQuality.FrameRate);
            Assert.True(adjustedQuality.BitRate > initialQuality.BitRate);
        }

        [Fact]
        public async Task GetQualityPresets_ShouldReturnAllPresets()
        {
            // Act
            var presets = await _service.GetQualityPresetsAsync();

            // Assert
            Assert.NotNull(presets);
            Assert.True(presets.Count() >= 3); // At least Low, Medium, High
            
            var lowPreset = presets.FirstOrDefault(p => p.Name == "Low");
            Assert.NotNull(lowPreset);
            Assert.True(lowPreset.Settings.Resolution <= 720);
            
            var highPreset = presets.FirstOrDefault(p => p.Name == "High");
            Assert.NotNull(highPreset);
            Assert.True(highPreset.Settings.Resolution >= 1080);
        }

        [Fact]
        public async Task CalculateNetworkScore_PerfectConditions_ShouldReturn100()
        {
            // Arrange
            var metrics = new SessionMetrics
            {
                BandwidthBps = 100_000_000, // 100 Mbps
                LatencyMs = 1,
                PacketLoss = 0.0f,
                Jitter = 0.1f
            };

            // Act
            var score = await _service.CalculateNetworkScoreAsync(metrics);

            // Assert
            Assert.Equal(100, score);
        }

        [Fact]
        public async Task CalculateNetworkScore_PoorConditions_ShouldReturnLowScore()
        {
            // Arrange
            var metrics = new SessionMetrics
            {
                BandwidthBps = 100_000, // 100 Kbps
                LatencyMs = 500,
                PacketLoss = 0.10f, // 10% loss
                Jitter = 50.0f
            };

            // Act
            var score = await _service.CalculateNetworkScoreAsync(metrics);

            // Assert
            Assert.True(score < 30); // Poor conditions should yield low score
        }

        [Fact]
        public async Task SuggestQualitySettings_ForDevice_ShouldConsiderDeviceCapabilities()
        {
            // Arrange
            var deviceInfo = new DeviceCapabilities
            {
                MaxResolution = 1920,
                MaxFrameRate = 60,
                SupportsHardwareEncoding = true,
                AvailableEncoders = new[] { "h264", "h265" },
                CpuCores = 8,
                MemoryMb = 16384
            };

            var networkMetrics = new SessionMetrics
            {
                BandwidthBps = 5_000_000,
                LatencyMs = 20,
                PacketLoss = 0.01f
            };

            // Act
            var quality = await _service.SuggestQualitySettingsAsync(deviceInfo, networkMetrics);

            // Assert
            Assert.NotNull(quality);
            Assert.True(quality.Resolution <= deviceInfo.MaxResolution);
            Assert.True(quality.FrameRate <= deviceInfo.MaxFrameRate);
            Assert.True(quality.UseHardwareEncoding);
            Assert.Contains(quality.Encoder, deviceInfo.AvailableEncoders);
        }

        [Fact]
        public async Task MonitorQualityTrends_ShouldDetectDegradation()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var historicalMetrics = new List<SessionMetrics>
            {
                new() { BandwidthBps = 5_000_000, Timestamp = DateTime.UtcNow.AddMinutes(-5) },
                new() { BandwidthBps = 4_000_000, Timestamp = DateTime.UtcNow.AddMinutes(-4) },
                new() { BandwidthBps = 3_000_000, Timestamp = DateTime.UtcNow.AddMinutes(-3) },
                new() { BandwidthBps = 2_000_000, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
                new() { BandwidthBps = 1_500_000, Timestamp = DateTime.UtcNow.AddMinutes(-1) }
            };

            _mockMetricsService.Setup(x => x.GetHistoricalMetricsAsync(sessionId, It.IsAny<TimeSpan>()))
                .ReturnsAsync(historicalMetrics);

            // Act
            var trend = await _service.AnalyzeQualityTrendAsync(sessionId, TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(trend);
            Assert.Equal(QualityTrend.Degrading, trend.Trend);
            Assert.True(trend.ShouldPreemptivelyReduce);
        }

        [Fact]
        public async Task ApplyQualitySettings_ShouldUpdateSession()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var quality = new QualitySettings
            {
                Resolution = 1080,
                FrameRate = 30,
                BitRate = 5_000_000,
                Encoder = "h264",
                KeyFrameInterval = 60,
                Preset = QualityPreset.High
            };

            // Act
            var result = await _service.ApplyQualitySettingsAsync(sessionId, quality);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(quality.Resolution, result.AppliedSettings?.Resolution);
            Assert.Equal(quality.FrameRate, result.AppliedSettings?.FrameRate);
            Assert.Equal(quality.BitRate, result.AppliedSettings?.BitRate);
        }

        [Theory]
        [InlineData(1_000_000, 720, 15)]    // 1 Mbps -> 720p15
        [InlineData(3_000_000, 1080, 30)]   // 3 Mbps -> 1080p30
        [InlineData(5_000_000, 1080, 30)]   // 5 Mbps -> 1080p30
        [InlineData(10_000_000, 1080, 60)]  // 10 Mbps -> 1080p60
        public async Task DetermineOptimalQuality_VariousBandwidths_ShouldScaleAppropriately(
            long bandwidthBps, int expectedResolution, int expectedFrameRate)
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var metrics = new SessionMetrics
            {
                BandwidthBps = bandwidthBps,
                LatencyMs = 20,
                PacketLoss = 0.01f,
                CpuUsage = 30,
                MemoryUsageMb = 2048
            };

            _mockMetricsService.Setup(x => x.GetSessionMetricsAsync(sessionId))
                .ReturnsAsync(metrics);

            // Act
            var quality = await _service.DetermineOptimalQualityAsync(sessionId);

            // Assert
            Assert.NotNull(quality);
            Assert.Equal(expectedResolution, quality.Resolution);
            Assert.Equal(expectedFrameRate, quality.FrameRate);
        }
    }
}