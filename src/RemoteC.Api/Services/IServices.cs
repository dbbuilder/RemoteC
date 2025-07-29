using RemoteC.Shared.Models;

namespace RemoteC.Api.Services;

/// <summary>
/// Interface for session management operations
/// </summary>
public interface ISessionService
{
    Task<IEnumerable<SessionDto>> GetUserSessionsAsync(string userId);
    Task<SessionDto?> GetSessionAsync(Guid sessionId, string userId);
    Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, string userId);
    Task<SessionStartResult> StartSessionAsync(Guid sessionId, string userId);
    Task StopSessionAsync(Guid sessionId, string userId);
    Task<PinGenerationResult> GeneratePinAsync(Guid sessionId, string userId);
    Task<bool> ValidatePinAsync(Guid sessionId, string pin);
}

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetUserAsync(string userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request);
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
    Task<bool> HasPermissionAsync(string userId, string permission);
}

/// <summary>
/// Interface for PIN code operations
/// </summary>
public interface IPinService
{
    Task<string> GeneratePinAsync(Guid sessionId);
    Task<bool> ValidatePinAsync(Guid sessionId, string pin);
    Task InvalidatePinAsync(Guid sessionId);
    Task<bool> IsPinValidAsync(Guid sessionId, string pin);
}

/// <summary>
/// Interface for remote control operations (ControlR integration)
/// </summary>
public interface IRemoteControlService
{
    Task<string> StartRemoteSessionAsync(Guid sessionId, string deviceId);
    Task StopRemoteSessionAsync(Guid sessionId);
    Task<bool> SendInputAsync(Guid sessionId, object inputData);
    Task<byte[]> GetScreenshotAsync(Guid sessionId);
    Task<bool> IsSessionActiveAsync(Guid sessionId);
}

/// <summary>
/// Interface for command execution operations
/// </summary>
public interface ICommandExecutionService
{
    Task<CommandExecutionResult> ExecuteCommandAsync(Guid sessionId, string command, string shell = "powershell");
    Task<IEnumerable<CommandHistoryDto>> GetCommandHistoryAsync(Guid sessionId);
    Task<bool> IsCommandAllowedAsync(string command);
}

/// <summary>
/// Interface for file transfer operations
/// </summary>
public interface IFileTransferService
{
    Task<FileTransferResult> StartFileTransferAsync(Guid sessionId, FileTransferRequest request);
    Task<FileTransferStatus> GetTransferStatusAsync(Guid transferId);
    Task CancelTransferAsync(Guid transferId);
    Task<IEnumerable<FileTransferHistoryDto>> GetTransferHistoryAsync(Guid sessionId);
}

/// <summary>
/// Interface for audit logging operations
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(string action, string? entityType = null, string? entityId = null, 
        string? userId = null, object? oldValues = null, object? newValues = null);
    Task LogSecurityEventAsync(string eventType, string userId, string? details = null);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null, 
        string? userId = null, string? action = null);
}

// Additional DTOs for services
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AzureAdB2CId { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? Roles { get; set; }
}

public class CommandExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? ErrorOutput { get; set; }
    public int ExitCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime ExecutedAt { get; set; }
}

public class CommandHistoryDto
{
    public Guid Id { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Shell { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
}

public class FileTransferRequest
{
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public FileTransferDirection Direction { get; set; }
    public long FileSize { get; set; }
}

public class FileTransferResult
{
    public Guid TransferId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FileTransferStatus
{
    public Guid TransferId { get; set; }
    public FileTransferState State { get; set; }
    public long BytesTransferred { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FileTransferHistoryDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public FileTransferDirection Direction { get; set; }
    public long FileSize { get; set; }
    public FileTransferState FinalState { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string TransferredBy { get; set; } = string.Empty;
}

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

public enum FileTransferDirection
{
    Upload,
    Download
}

public enum FileTransferState
{
    Queued,
    InProgress,
    Completed,
    Failed,
    Cancelled
}