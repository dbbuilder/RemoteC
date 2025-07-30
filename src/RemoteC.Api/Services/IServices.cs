using RemoteC.Shared.Models;
using RemoteC.Data.Entities;

namespace RemoteC.Api.Services;

/// <summary>
/// Interface for session management operations
/// </summary>
/// <remarks>
/// Provides methods for creating, managing, and monitoring remote control sessions.
/// Includes PIN-based authentication for quick device access.
/// </remarks>
public interface ISessionService
{
    /// <summary>
    /// Gets all sessions for a specific user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>Collection of sessions accessible by the user</returns>
    Task<IEnumerable<SessionDto>> GetUserSessionsAsync(string userId);
    
    /// <summary>
    /// Gets a specific session by ID
    /// </summary>
    /// <param name="sessionId">The session's unique identifier</param>
    /// <param name="userId">The user's unique identifier for authorization</param>
    /// <returns>Session details if found and user has access, otherwise null</returns>
    Task<SessionDto?> GetSessionAsync(Guid sessionId, string userId);
    
    /// <summary>
    /// Creates a new remote control session
    /// </summary>
    /// <param name="request">Session creation parameters</param>
    /// <param name="userId">The user creating the session</param>
    /// <returns>The created session details</returns>
    Task<SessionDto> CreateSessionAsync(CreateSessionRequest request, string userId);
    
    /// <summary>
    /// Starts a remote control session
    /// </summary>
    /// <param name="sessionId">The session to start</param>
    /// <param name="userId">The user starting the session</param>
    /// <returns>Session start result including connection details</returns>
    Task<SessionStartResult> StartSessionAsync(Guid sessionId, string userId);
    
    /// <summary>
    /// Stops an active remote control session
    /// </summary>
    /// <param name="sessionId">The session to stop</param>
    /// <param name="userId">The user stopping the session</param>
    /// <returns>Task completion</returns>
    Task StopSessionAsync(Guid sessionId, string userId);
    
    /// <summary>
    /// Generates a PIN for quick session access
    /// </summary>
    /// <param name="sessionId">The session to generate PIN for</param>
    /// <param name="userId">The user requesting the PIN</param>
    /// <returns>PIN generation result including the PIN code</returns>
    Task<PinGenerationResult> GeneratePinAsync(Guid sessionId, string userId);
    
    /// <summary>
    /// Validates a PIN for session access
    /// </summary>
    /// <param name="sessionId">The session to validate PIN for</param>
    /// <param name="pin">The PIN code to validate</param>
    /// <returns>True if PIN is valid, false otherwise</returns>
    Task<bool> ValidatePinAsync(Guid sessionId, string pin);
}

/// <summary>
/// Interface for user management operations
/// </summary>
/// <remarks>
/// Provides methods for user CRUD operations, permission management, and Azure AD B2C integration.
/// </remarks>
public interface IUserService
{
    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>User details if found, otherwise null</returns>
    Task<UserDto?> GetUserAsync(string userId);
    
    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">User creation parameters</param>
    /// <returns>The created user details</returns>
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    
    /// <summary>
    /// Updates an existing user's information
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="request">Updated user information</param>
    /// <returns>The updated user details</returns>
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request);
    
    /// <summary>
    /// Gets all permissions assigned to a user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>Collection of permission names</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
    
    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="permission">The permission to check</param>
    /// <returns>True if user has the permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(string userId, string permission);
    
    /// <summary>
    /// Creates or updates a user from Azure AD B2C claims
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="lastName">User's last name</param>
    /// <param name="azureId">Azure AD B2C identifier</param>
    /// <returns>The created or updated user details</returns>
    Task<UserDto> CreateOrUpdateUserAsync(string email, string firstName, string lastName, string azureId);
}

/// <summary>
/// Interface for PIN code operations
/// </summary>
/// <remarks>
/// Manages time-limited PIN codes for quick device access without full authentication.
/// PINs are single-use and expire after a configured duration.
/// </remarks>
public interface IPinService
{
    /// <summary>
    /// Generates a new PIN for a session
    /// </summary>
    /// <param name="sessionId">The session to generate PIN for</param>
    /// <returns>The generated PIN code</returns>
    Task<string> GeneratePinAsync(Guid sessionId);
    
    /// <summary>
    /// Validates and consumes a PIN
    /// </summary>
    /// <param name="sessionId">The session to validate PIN for</param>
    /// <param name="pin">The PIN code to validate</param>
    /// <returns>True if PIN is valid and was consumed, false otherwise</returns>
    Task<bool> ValidatePinAsync(Guid sessionId, string pin);
    
    /// <summary>
    /// Invalidates an existing PIN
    /// </summary>
    /// <param name="sessionId">The session whose PIN should be invalidated</param>
    /// <returns>Task completion</returns>
    Task InvalidatePinAsync(Guid sessionId);
    
    /// <summary>
    /// Checks if a PIN is valid without consuming it
    /// </summary>
    /// <param name="sessionId">The session to check PIN for</param>
    /// <param name="pin">The PIN code to check</param>
    /// <returns>True if PIN is valid, false otherwise</returns>
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


