namespace RemoteC.Shared.Models;

public class SessionDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public SessionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? ConnectionInfo { get; set; }
    public bool RequirePin { get; set; }
    
    // Device information
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? OperatingSystem { get; set; }
    
    // Creator information
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    
    // Participants
    public IEnumerable<SessionParticipantInfo> Participants { get; set; } = new List<SessionParticipantInfo>();
}

public class SessionSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public SessionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
}

public class SessionParticipantInfo
{
    public Guid Id { get; set; }
    public ParticipantRole Role { get; set; }
    public bool IsConnected { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}