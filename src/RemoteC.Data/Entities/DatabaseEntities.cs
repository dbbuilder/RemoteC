using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities;

/// <summary>
/// User entity representing application users
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? AzureAdB2CId { get; set; }
    
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsSuperAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastLoginDate { get; set; }
    
    [MaxLength(45)]
    public string? LastLoginIp { get; set; }
    
    [MaxLength(100)]
    public string? Department { get; set; }
    
    public Guid? OrganizationId { get; set; }
    
    // Computed property
    public string Name => $"{FirstName} {LastName}";
    
    // Navigation properties
    public virtual Organization? Organization { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Session> CreatedSessions { get; set; } = new List<Session>();
    public virtual ICollection<SessionParticipant> SessionParticipants { get; set; } = new List<SessionParticipant>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Role entity for role-based access control
/// </summary>
public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Junction entity for User-Role many-to-many relationship
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid AssignedBy { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

/// <summary>
/// Permission entity for granular access control
/// </summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string Resource { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Junction entity for Role-Permission many-to-many relationship
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public Guid GrantedBy { get; set; }
    
    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

/// <summary>
/// Device entity representing target devices for remote control
/// </summary>
public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? HostName { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(17)]
    public string? MacAddress { get; set; }
    
    [MaxLength(100)]
    public string? OperatingSystem { get; set; }
    
    [MaxLength(50)]
    public string? Version { get; set; }
    
    public bool IsOnline { get; set; } = false;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public Guid? RegisteredBy { get; set; }
    public Guid? OrganizationId { get; set; }
    
    // Navigation properties
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<DeviceGroupMember> DeviceGroupMembers { get; set; } = new List<DeviceGroupMember>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}

/// <summary>
/// Device group entity for organizing devices
/// </summary>
public class DeviceGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    // Navigation properties
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<DeviceGroupMember> DeviceGroupMembers { get; set; } = new List<DeviceGroupMember>();
}

/// <summary>
/// Junction entity for Device-DeviceGroup many-to-many relationship
/// </summary>
public class DeviceGroupMember
{
    public Guid DeviceGroupId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public Guid AddedBy { get; set; }
    
    // Navigation properties
    public virtual DeviceGroup DeviceGroup { get; set; } = null!;
    public virtual Device Device { get; set; } = null!;
}

/// <summary>
/// Session entity representing remote control sessions
/// </summary>
public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public Guid DeviceId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid OrganizationId { get; set; }
    
    public SessionStatus Status { get; set; } = SessionStatus.Created;
    public SessionType Type { get; set; } = SessionType.RemoteControl;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    public string? ConnectionInfo { get; set; }
    public bool RequirePin { get; set; } = true;
    
    // Navigation properties
    public virtual Device Device { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<SessionParticipant> Participants { get; set; } = new List<SessionParticipant>();
    public virtual ICollection<SessionLog> Logs { get; set; } = new List<SessionLog>();
}

/// <summary>
/// Session participant entity
/// </summary>
public class SessionParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    
    public ParticipantRole Role { get; set; } = ParticipantRole.Viewer;
    public bool IsConnected { get; set; } = false;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    
    // Navigation properties
    public virtual Session Session { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Session log entity for audit trail
/// </summary>
public class SessionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? DeviceId { get; set; }
    
    [MaxLength(100)]
    public string? EventType { get; set; }
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public LogLevel Level { get; set; } = LogLevel.Information;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? Details { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(50)]
    public string? Location { get; set; }
    
    [MaxLength(50)]
    public string? DeviceType { get; set; }
    
    public double? Latency { get; set; }
    
    public string? AdditionalData { get; set; }
    
    // Navigation properties
    public virtual Session Session { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual Device? Device { get; set; }
}

/// <summary>
/// Session metrics entity for performance tracking
/// </summary>
public class SessionMetrics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    
    public double LatencyMs { get; set; }
    public double FrameRate { get; set; }
    public long BytesTransferred { get; set; }
    public double CompressionRatio { get; set; }
    public int QualityLevel { get; set; }
    
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Session Session { get; set; } = null!;
}

/// <summary>
/// Audit log entity for security and compliance
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? EntityType { get; set; }
    
    [MaxLength(100)]
    public string? EntityId { get; set; }
    
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ResourceId { get; set; }
    
    [MaxLength(200)]
    public string? ResourceName { get; set; }
    
    public Guid? UserId { get; set; }
    
    [MaxLength(100)]
    public string? UserName { get; set; }
    
    [MaxLength(255)]
    public string? UserEmail { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(1000)]
    public string? UserAgent { get; set; }
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Details { get; set; }
    public string? Metadata { get; set; }
    public string? CorrelationId { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? StackTrace { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    
    public Guid? OrganizationId { get; set; }
    public int Severity { get; set; }
    public int Category { get; set; }
    
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Organization? Organization { get; set; }
}

/// <summary>
/// PIN code entity for device access
/// </summary>
public class PinCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Pin { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string HashedPin { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    
    public bool SmsDelivered { get; set; } = false;
    public bool EmailDelivered { get; set; } = false;
    
    // Navigation properties
    public virtual Session Session { get; set; } = null!;
}

// Enumerations
public enum SessionStatus
{
    Created = 0,
    WaitingForPin = 1,
    Connecting = 2,
    Connected = 3,
    Active = 4,
    Paused = 5,
    Disconnected = 6,
    Ended = 7,
    Error = 8,
    Completed = 9
}

public enum SessionType
{
    RemoteControl = 0,
    ViewOnly = 1,
    FileTransfer = 2,
    CommandExecution = 3
}

public enum ParticipantRole
{
    Viewer = 0,
    Controller = 1,
    Administrator = 2,
    Owner = 3
}

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}