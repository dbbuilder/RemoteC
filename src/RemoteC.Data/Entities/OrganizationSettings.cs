using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities;

/// <summary>
/// Organization settings entity for configuration
/// </summary>
public class OrganizationSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    
    public bool AutoRecordSessions { get; set; }
    public int MaxRecordingDuration { get; set; } = 3600; // seconds
    public long MaxRecordingSize { get; set; } = 1073741824; // 1GB
    public int RetentionDays { get; set; } = 30;
    public bool SessionRecordingEnabled { get; set; } = true;
    public bool AllowPinAccess { get; set; } = true;
    public int SessionRecordingDays { get; set; } = 30; // How long to keep recordings
    
    [MaxLength(1000)]
    public string? IpWhitelist { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
}