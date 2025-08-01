using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Quality settings for remote session
    /// </summary>
    public class QualitySettings
    {
        /// <summary>
        /// Vertical resolution (e.g., 720, 1080, 1440)
        /// </summary>
        public int Resolution { get; set; }

        /// <summary>
        /// Frames per second
        /// </summary>
        public int FrameRate { get; set; }

        /// <summary>
        /// Target bitrate in bits per second
        /// </summary>
        public long BitRate { get; set; }

        /// <summary>
        /// Video encoder to use (h264, h265, vp8, vp9)
        /// </summary>
        public string Encoder { get; set; } = "h264";

        /// <summary>
        /// Quality preset
        /// </summary>
        public QualityPreset Preset { get; set; }

        /// <summary>
        /// Encoding profile
        /// </summary>
        public EncodingProfile EncodingProfile { get; set; }

        /// <summary>
        /// Keyframe interval in frames
        /// </summary>
        public int KeyFrameInterval { get; set; } = 60;

        /// <summary>
        /// Enable B-frames for better compression
        /// </summary>
        public bool EnableBFrames { get; set; } = true;

        /// <summary>
        /// Use hardware encoding if available
        /// </summary>
        public bool UseHardwareEncoding { get; set; } = true;

        /// <summary>
        /// Enable Forward Error Correction
        /// </summary>
        public bool EnableFEC { get; set; }

        /// <summary>
        /// JPEG quality for still images (0-100)
        /// </summary>
        public int JpegQuality { get; set; } = 85;

        /// <summary>
        /// Enable adaptive bitrate
        /// </summary>
        public bool EnableAdaptiveBitrate { get; set; } = true;

        /// <summary>
        /// Color depth in bits
        /// </summary>
        public int ColorDepth { get; set; } = 24;

        /// <summary>
        /// Enable frame skipping when behind
        /// </summary>
        public bool EnableFrameSkipping { get; set; } = true;
    }

    /// <summary>
    /// Predefined quality presets
    /// </summary>
    public enum QualityPreset
    {
        /// <summary>
        /// Lowest quality for very poor connections
        /// </summary>
        VeryLow,

        /// <summary>
        /// Low quality for slow connections
        /// </summary>
        Low,

        /// <summary>
        /// Balanced quality and performance
        /// </summary>
        Medium,

        /// <summary>
        /// High quality for good connections
        /// </summary>
        High,

        /// <summary>
        /// Maximum quality for excellent connections
        /// </summary>
        VeryHigh,

        /// <summary>
        /// Custom settings
        /// </summary>
        Custom
    }

    /// <summary>
    /// Quality level for adaptive quality
    /// </summary>
    public enum QualityLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Encoding profile for different use cases
    /// </summary>
    public enum EncodingProfile
    {
        /// <summary>
        /// Optimized for lowest latency
        /// </summary>
        LowLatency,

        /// <summary>
        /// Balanced latency and quality
        /// </summary>
        Balanced,

        /// <summary>
        /// Optimized for best quality
        /// </summary>
        HighQuality,

        /// <summary>
        /// Optimized for screen content (text, UI)
        /// </summary>
        ScreenContent,

        /// <summary>
        /// Optimized for video/motion content
        /// </summary>
        VideoContent
    }

    /// <summary>
    /// Session metrics for quality decisions
    /// </summary>
    public class SessionMetrics
    {
        public Guid SessionId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Available bandwidth in bits per second
        /// </summary>
        public long BandwidthBps { get; set; }

        /// <summary>
        /// Round-trip latency in milliseconds
        /// </summary>
        public double LatencyMs { get; set; }

        /// <summary>
        /// Packet loss percentage (0.0 - 1.0)
        /// </summary>
        public float PacketLoss { get; set; }

        /// <summary>
        /// Network jitter in milliseconds
        /// </summary>
        public float Jitter { get; set; }

        /// <summary>
        /// CPU usage percentage (0-100)
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Memory usage in MB
        /// </summary>
        public long MemoryUsageMb { get; set; }

        /// <summary>
        /// GPU usage percentage if available
        /// </summary>
        public double? GpuUsage { get; set; }

        /// <summary>
        /// Current frames per second
        /// </summary>
        public double CurrentFps { get; set; }

        /// <summary>
        /// Dropped frames count
        /// </summary>
        public long DroppedFrames { get; set; }

        /// <summary>
        /// Encoding time per frame in ms
        /// </summary>
        public double EncodingTimeMs { get; set; }
    }

    /// <summary>
    /// Device capabilities for quality optimization
    /// </summary>
    public class DeviceCapabilities
    {
        /// <summary>
        /// Maximum supported resolution
        /// </summary>
        public int MaxResolution { get; set; }

        /// <summary>
        /// Maximum supported frame rate
        /// </summary>
        public int MaxFrameRate { get; set; }

        /// <summary>
        /// Supports hardware encoding
        /// </summary>
        public bool SupportsHardwareEncoding { get; set; }

        /// <summary>
        /// Available video encoders
        /// </summary>
        public string[] AvailableEncoders { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Number of CPU cores
        /// </summary>
        public int CpuCores { get; set; }

        /// <summary>
        /// Available memory in MB
        /// </summary>
        public long MemoryMb { get; set; }

        /// <summary>
        /// GPU model if available
        /// </summary>
        public string? GpuModel { get; set; }

        /// <summary>
        /// Network interface speed in Mbps
        /// </summary>
        public int NetworkSpeedMbps { get; set; }
    }

    /// <summary>
    /// Quality preset definition
    /// </summary>
    public class QualityPresetDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QualitySettings Settings { get; set; } = new();
        public long MinBandwidthBps { get; set; }
        public long RecommendedBandwidthBps { get; set; }
    }

    /// <summary>
    /// Quality adjustment result
    /// </summary>
    public class QualityAdjustmentResult
    {
        public bool Success { get; set; }
        public QualitySettings? AppliedSettings { get; set; }
        public string? Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Quality trend analysis
    /// </summary>
    public class QualityTrendAnalysis
    {
        public QualityTrend Trend { get; set; }
        public double TrendStrength { get; set; }
        public bool ShouldPreemptivelyReduce { get; set; }
        public bool ShouldAttemptIncrease { get; set; }
        public TimeSpan StableDuration { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Quality trend direction
    /// </summary>
    public enum QualityTrend
    {
        Stable,
        Improving,
        Degrading,
        Fluctuating
    }

    /// <summary>
    /// Adaptive quality configuration
    /// </summary>
    public class AdaptiveQualityConfig
    {
        /// <summary>
        /// Enable automatic quality adjustment
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Interval between quality checks in seconds
        /// </summary>
        public int CheckIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Minimum time before upgrading quality in seconds
        /// </summary>
        public int UpgradeDelaySeconds { get; set; } = 10;

        /// <summary>
        /// Minimum time before downgrading quality in seconds
        /// </summary>
        public int DowngradeDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Target buffer size in milliseconds
        /// </summary>
        public int TargetBufferMs { get; set; } = 100;

        /// <summary>
        /// Maximum quality changes per minute
        /// </summary>
        public int MaxChangesPerMinute { get; set; } = 6;

        /// <summary>
        /// Bandwidth safety margin percentage
        /// </summary>
        public double BandwidthMarginPercent { get; set; } = 0.8;

        /// <summary>
        /// CPU usage threshold for downgrade
        /// </summary>
        public double CpuThresholdPercent { get; set; } = 80;

        /// <summary>
        /// Memory usage threshold for downgrade in MB
        /// </summary>
        public long MemoryThresholdMb { get; set; } = 500;
    }
}