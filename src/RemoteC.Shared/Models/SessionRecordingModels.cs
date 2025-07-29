using System;

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

    public class SessionRecordingOptions
    {
        public string StoragePath { get; set; } = string.Empty;
        public string StorageConnectionString { get; set; } = string.Empty;
        public bool EnableRecording { get; set; } = true;
        public TimeSpan MaxRecordingDuration { get; set; } = TimeSpan.FromHours(8);
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
        public CompressionType DefaultCompressionType { get; set; } = CompressionType.Gzip;
        public RecordingQuality DefaultQuality { get; set; } = RecordingQuality.High;
        public int DefaultFrameRate { get; set; } = 30;
    }
}