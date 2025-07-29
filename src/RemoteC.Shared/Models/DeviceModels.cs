namespace RemoteC.Shared.Models;

/// <summary>
/// Device data transfer object
/// </summary>
public class DeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Version { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>
/// Session PIN request model
/// </summary>
public class SessionPinRequest
{
    public Guid SessionId { get; set; }
}

/// <summary>
/// Session PIN response model
/// </summary>
public class SessionPinResponse
{
    public string Pin { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}