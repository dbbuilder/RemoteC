namespace RemoteC.Shared.Models;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum AuditSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public enum AuditCategory
{
    General = 0,
    Authentication = 1,
    Authorization = 2,
    DataAccess = 3,
    DataModification = 4,
    Configuration = 5,
    Security = 6,
    Compliance = 7,
    SystemConfiguration = 8
}