using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    public class FileTransferRequest
    {
        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public TransferDirection Direction { get; set; }
        public string? Checksum { get; set; }
        public FileMetadata? Metadata { get; set; }
    }

    public class FileMetadata
    {
        public string MimeType { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public Dictionary<string, string>? CustomMetadata { get; set; }
    }

    public class FileChunk
    {
        public Guid TransferId { get; set; }
        public int ChunkNumber { get; set; }
        public int ChunkIndex { get; set; } // Alias for ChunkNumber for compatibility
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string Checksum { get; set; } = string.Empty;
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
    }

    public class ChunkUploadResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; } // Alias for ErrorMessage
        public string? ErrorMessage { get; set; }
        public int ReceivedChunks { get; set; } // Alias for ChunksReceived
        public int ChunksReceived { get; set; }
        public int TotalChunks { get; set; }
        public long BytesReceived { get; set; }
        public int Progress { get; set; }
        public bool IsComplete { get; set; }
    }

    public class TransferStatusInfo
    {
        public Guid TransferId { get; set; }
        public TransferStatus Status { get; set; }
        public int ChunksReceived { get; set; }
        public int TotalChunks { get; set; }
        public int[] MissingChunks { get; set; } = Array.Empty<int>();
        public int Progress { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
    }

    public class FileTransferOptions
    {
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
        public long MaxFileSize { get; set; } = 5L * 1024 * 1024 * 1024; // 5GB
        public string StoragePath { get; set; } = string.Empty;
        public bool EnableEncryption { get; set; } = true;
        public bool EncryptionEnabled { get; set; } = true; // Alias for EnableEncryption
        public bool EnableCompression { get; set; } = true;
        public bool CompressionEnabled { get; set; } = true; // Alias for EnableCompression
        public int MaxConcurrentTransfers { get; set; } = 10;
        public int ChunkRetryCount { get; set; } = 3;
        public int ChunkRetryDelayMs { get; set; } = 1000;
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public TimeSpan ChunkTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    public enum TransferDirection
    {
        Upload,
        Download
    }

    public enum TransferStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Queued
    }

    public class FileTransferMetrics
    {
        public Guid TransferId { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public int ChunksTransferred { get; set; }
        public int TotalChunks { get; set; }
        public double TransferRate { get; set; } // Bytes per second
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public int RetryCount { get; set; }
        public TransferStatus Status { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public class FileTransfer
    {
        public Guid TransferId { get; set; }
        public Guid SessionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public TransferDirection Direction { get; set; }
        public TransferStatus Status { get; set; }
        public int TotalChunks { get; set; }
        public int ReceivedChunks { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public FileMetadata? Metadata { get; set; }
        public string? Error { get; set; }
    }
}