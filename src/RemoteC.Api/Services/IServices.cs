using RemoteC.Shared.Models;
using RemoteC.Data.Entities;

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
    Task<UserDto> CreateOrUpdateUserAsync(string email, string firstName, string lastName, string azureId);
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
    Task<FileTransfer> InitiateTransferAsync(FileTransferRequest request);
    Task<ChunkUploadResult> UploadChunkAsync(FileChunk chunk);
    Task<FileTransfer?> GetTransferStatusAsync(Guid transferId);
    Task<IEnumerable<int>> GetMissingChunksAsync(Guid transferId);
    Task<FileChunk?> DownloadChunkAsync(Guid transferId, int chunkIndex);
    Task<bool> CancelTransferAsync(Guid transferId);
    Task<FileTransferMetrics> GetTransferMetricsAsync(Guid transferId);
    Task CleanupExpiredTransfersAsync();
    Task<int> CleanupStalledTransfersAsync();
}


