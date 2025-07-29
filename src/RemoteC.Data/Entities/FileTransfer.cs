using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    public class FileTransfer
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public long TotalSize { get; set; }

        public int ChunkSize { get; set; }

        public int TotalChunks { get; set; }

        public int ChunksReceived { get; set; }

        public long BytesReceived { get; set; }

        public TransferDirection Direction { get; set; }

        public TransferStatus Status { get; set; }
        
        // Added for test compatibility
        public double Progress => TotalChunks > 0 ? (double)ChunksReceived / TotalChunks * 100 : 0;
        public string MissingChunks { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? EncryptionKeyId { get; set; }

        [MaxLength(255)]
        public string? Checksum { get; set; }

        [MaxLength(500)]
        public string? SourcePath { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public virtual Session Session { get; set; } = null!;
        public virtual User User { get; set; } = null!;
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
        Cancelled
    }
}