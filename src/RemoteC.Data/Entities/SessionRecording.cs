using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities;

/// <summary>
/// Session recording entity for storing recorded sessions
/// </summary>
public class SessionRecording
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? StoragePath { get; set; }
    
    public long FileSize { get; set; }
    public long FileSizeBytes => FileSize; // Alias for test compatibility
    public TimeSpan Duration { get; set; }
    
    [MaxLength(50)]
    public string? Format { get; set; }
    
    public bool IsEncrypted { get; set; }
    public bool IsCompressed { get; set; }
    
    public Guid OrganizationId { get; set; }
    public string? EncryptionKeyId { get; set; }
    public RemoteC.Shared.Models.CompressionType CompressionType { get; set; }
    public bool IncludeAudio { get; set; }
    public RemoteC.Shared.Models.RecordingQuality Quality { get; set; }
    public int FrameRate { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public RecordingStatus Status { get; set; } = RecordingStatus.InProgress;
    
    // Additional properties for recording management
    public int FrameCount { get; set; }
    public int ChunkCount { get; set; }
    public long TotalSize { get; set; }
    
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
    
    // Navigation properties
    public virtual Session Session { get; set; } = null!;
}

public enum RecordingStatus
{
    Recording = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Processing = 4,
    Archived = 5
}