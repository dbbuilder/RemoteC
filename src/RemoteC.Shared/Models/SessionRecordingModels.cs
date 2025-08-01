using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    public enum RecordingQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    public enum CompressionType
    {
        None = 0,
        Gzip = 1,
        Jpeg = 2,
        Png = 3,
        WebP = 4,
        H264 = 5,
        H265 = 6,
        VP8 = 7,
        VP9 = 8
    }
    
    public enum ExportQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Maximum = 3
    }

    public enum RecordingStatus
    {
        Pending = 0,
        Recording = 1,
        Paused = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    public enum FrameType
    {
        Video = 0,
        Audio = 1,
        KeyboardEvent = 2,
        MouseEvent = 3,
        Metadata = 4
    }

    public enum RecordingInputEventType
    {
        KeyDown = 0,
        KeyUp = 1,
        MouseMove = 2,
        MouseDown = 3,
        MouseUp = 4,
        MouseClick = 5,
        MouseWheel = 6
    }

    public enum RecordingExportFormat
    {
        MP4 = 0,
        WebM = 1,
        AVI = 2,
        Raw = 3
    }

    public enum TimelineEventType
    {
        VideoFrame = 0,
        AudioFrame = 1,
        KeyboardInput = 2,
        MouseInput = 3,
        SessionStart = 4,
        SessionEnd = 5,
        Bookmark = 6
    }

    public class SessionRecordingOptions
    {
        public bool Enabled { get; set; } = true;
        public string StoragePath { get; set; } = string.Empty;
        public string StorageConnectionString { get; set; } = string.Empty;
        public bool EnableRecording { get; set; } = true;
        public TimeSpan MaxRecordingDuration { get; set; } = TimeSpan.FromHours(8);
        public long MaxFileSizeBytes { get; set; } = 5L * 1024 * 1024 * 1024; // 5GB
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
        public CompressionType DefaultCompressionType { get; set; } = CompressionType.Gzip;
        public RecordingQuality DefaultQuality { get; set; } = RecordingQuality.High;
        public int DefaultFrameRate { get; set; } = 30;
        public string VideoCodec { get; set; } = "h264";
        public string AudioCodec { get; set; } = "aac";
        public int VideoBitrate { get; set; } = 2_000_000;
        public int AudioBitrate { get; set; } = 128_000;
        public int FrameRate { get; set; } = 30;
        public int KeyFrameInterval { get; set; } = 60;
        public int BufferSize { get; set; } = 10 * 1024 * 1024;
        public int RetentionDays { get; set; } = 30;
        public bool EnableCompression { get; set; } = true;
        public bool EnableEncryption { get; set; } = true;
    }

    public class StartRecordingRequest
    {
        public Guid SessionId { get; set; }
        public bool RecordVideo { get; set; } = true;
        public bool RecordAudio { get; set; } = true;
        public bool RecordKeyboard { get; set; } = true;
        public bool RecordMouse { get; set; } = true;
        public RecordingMetadata? Metadata { get; set; }
    }

    public class RecordingMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public Dictionary<string, string>? CustomData { get; set; }
    }

    public class SessionRecording
    {
        public Guid RecordingId { get; set; }
        public Guid SessionId { get; set; }
        public RecordingStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool RecordVideo { get; set; }
        public bool RecordAudio { get; set; }
        public bool RecordKeyboard { get; set; }
        public bool RecordMouse { get; set; }
        public RecordingMetadata? Metadata { get; set; }
        public long FileSize { get; set; }
        public int FrameCount { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class RecordingFrame
    {
        public DateTime Timestamp { get; set; }
        public FrameType Type { get; set; }
        public byte[]? Data { get; set; }
        public int FrameNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsKeyFrame { get; set; }
        public int AudioSampleRate { get; set; }
        public int AudioChannels { get; set; }
        public int AudioBitsPerSample { get; set; }
        public InputEventData? InputEvent { get; set; }
    }

    public class InputEventData
    {
        public RecordingInputEventType Type { get; set; }
        public string? Key { get; set; }
        public int KeyCode { get; set; }
        public string[]? Modifiers { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
        public int WheelDelta { get; set; }
    }

    public class RecordingPlaybackInfo
    {
        public Guid RecordingId { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalFrames { get; set; }
        public string PlaybackUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public RecordingBookmark[]? Bookmarks { get; set; }
    }

    public class RecordingSearchRequest
    {
        public string[]? Tags { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? SessionId { get; set; }
        public RecordingStatus? Status { get; set; }
    }

    public class RecordingExportResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public RecordingExportFormat Format { get; set; }
        public long FileSize { get; set; }
        public string? Error { get; set; }
    }

    public class RecordingTimeline
    {
        public Guid RecordingId { get; set; }
        public IEnumerable<TimelineEvent> Events { get; set; } = new List<TimelineEvent>();
    }

    public class TimelineEvent
    {
        public TimelineEventType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class RecordingThumbnail
    {
        public TimeSpan Timestamp { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class RecordingMetrics
    {
        public int VideoFrameCount { get; set; }
        public int AudioFrameCount { get; set; }
        public double AverageFrameRate { get; set; }
        public long VideoBitrate { get; set; }
        public long AudioBitrate { get; set; }
        public long TotalFileSize { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class RecordingBookmark
    {
        public TimeSpan Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "#FF5722";
    }

    public class RecordingOptions
    {
        public CompressionType CompressionType { get; set; } = CompressionType.Gzip;
        public bool IncludeAudio { get; set; } = true;
        public RecordingQuality Quality { get; set; } = RecordingQuality.High;
        public int FrameRate { get; set; } = 30;
    }

    public class RecordingExportOptions
    {
        public RecordingExportFormat Format { get; set; }
        public int Quality { get; set; }
        public bool IncludeAudio { get; set; }
    }
}