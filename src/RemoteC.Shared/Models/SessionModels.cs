using System.ComponentModel.DataAnnotations;

namespace RemoteC.Shared.Models;

/// <summary>
/// Data transfer object for session information
/// </summary>
public class SessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public SessionStatus Status { get; set; }
    public string? Pin { get; set; }
    public DateTime? PinExpiresAt { get; set; }
    public List<SessionParticipantDto> Participants { get; set; } = new();
    public SessionMetricsDto? Metrics { get; set; }
}

/// <summary>
/// Request object for creating a new session
/// </summary>
public class CreateSessionRequest
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    public SessionType Type { get; set; } = SessionType.RemoteControl;
    public bool RequirePin { get; set; } = true;
    public List<string> InvitedUsers { get; set; } = new();
}

/// <summary>
/// Result of session start operation
/// </summary>
public class SessionStartResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ConnectionUrl { get; set; }
    public string? Pin { get; set; }
    public Dictionary<string, object> ConnectionInfo { get; set; } = new();
}

/// <summary>
/// Session participant information
/// </summary>
public class SessionParticipantDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; }
    public bool IsConnected { get; set; }
    public DateTime JoinedAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Session performance metrics
/// </summary>
public class SessionMetricsDto
{
    public double LatencyMs { get; set; }
    public double FrameRate { get; set; }
    public long BytesTransferred { get; set; }
    public double CompressionRatio { get; set; }
    public TimeSpan Duration { get; set; }
    public int QualityLevel { get; set; }
}

/// <summary>
/// PIN generation result
/// </summary>
public class PinGenerationResult
{
    public string Pin { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool SmsDelivered { get; set; }
    public bool EmailDelivered { get; set; }
}

/// <summary>
/// Mouse input data
/// </summary>
public class MouseInputDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public MouseButton Button { get; set; }
    public MouseAction Action { get; set; }
    public int WheelDelta { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Keyboard input data
/// </summary>
public class KeyboardInputDto
{
    public string Key { get; set; } = string.Empty;
    public KeyAction Action { get; set; }
    public bool CtrlPressed { get; set; }
    public bool AltPressed { get; set; }
    public bool ShiftPressed { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Screen update data
/// </summary>
public class ScreenUpdateDto
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = "JPEG";
    public int Quality { get; set; }
    public DateTime Timestamp { get; set; }
    public List<ScreenRegionDto> ChangedRegions { get; set; } = new();
}

/// <summary>
/// Screen region information
/// </summary>
public class ScreenRegionDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Chat message data
/// </summary>
public class ChatMessageDto
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Session status enumeration
/// </summary>
public enum SessionStatus
{
    Created,
    WaitingForPin,
    Connecting,
    Connected,
    Active,
    Paused,
    Disconnected,
    Ended,
    Error
}

/// <summary>
/// Session type enumeration
/// </summary>
public enum SessionType
{
    RemoteControl,
    ViewOnly,
    FileTransfer,
    CommandExecution
}

/// <summary>
/// Participant role enumeration
/// </summary>
public enum ParticipantRole
{
    Viewer,
    Controller,
    Administrator,
    Owner
}

/// <summary>
/// Mouse button enumeration
/// </summary>
public enum MouseButton
{
    None,
    Left,
    Right,
    Middle,
    X1,
    X2
}

/// <summary>
/// Mouse action enumeration
/// </summary>
public enum MouseAction
{
    Move,
    Press,
    Release,
    Click,
    DoubleClick,
    Wheel
}

/// <summary>
/// Key action enumeration
/// </summary>
public enum KeyAction
{
    Press,
    Release,
    Type
}