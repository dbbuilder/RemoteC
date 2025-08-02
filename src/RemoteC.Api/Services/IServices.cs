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
    
    /// <summary>
    /// Updates the status of a session
    /// </summary>
    /// <param name="sessionId">The session to update</param>
    /// <param name="status">The new session status</param>
    /// <returns>Task completion</returns>
    Task UpdateSessionStatusAsync(Guid sessionId, RemoteC.Shared.Models.SessionStatus status);
    
    /// <summary>
    /// Joins a session using a PIN code
    /// </summary>
    /// <param name="sessionId">The session to join</param>
    /// <param name="pin">The PIN code</param>
    /// <param name="userId">The user joining the session</param>
    /// <returns>Session join result with connection details</returns>
    Task<SessionJoinResult> JoinSessionWithPinAsync(Guid sessionId, string pin, string userId);
    
    /// <summary>
    /// Generates a temporary PIN with custom expiration
    /// </summary>
    /// <param name="sessionId">The session to generate PIN for</param>
    /// <param name="userId">The user requesting the PIN</param>
    /// <param name="expirationMinutes">PIN expiration time in minutes</param>
    /// <returns>Extended PIN generation result</returns>
    Task<ExtendedPinGenerationResult> GenerateTemporaryPinAsync(Guid sessionId, string userId, int expirationMinutes);
    
    /// <summary>
    /// Validates if a PIN is valid before attempting to join
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="pin">The PIN to validate</param>
    /// <returns>True if PIN is valid</returns>
    Task<bool> ValidatePinBeforeJoinAsync(Guid sessionId, string pin);
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
    
    /// <summary>
    /// Generates a PIN with custom expiration
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="expirationMinutes">PIN expiration in minutes</param>
    /// <returns>PIN generation result</returns>
    Task<ExtendedPinGenerationResult> GeneratePinWithDetailsAsync(Guid sessionId, int expirationMinutes);
    
    /// <summary>
    /// Gets details about a PIN
    /// </summary>
    /// <param name="pin">The PIN code</param>
    /// <returns>PIN details or null if not found</returns>
    Task<PinDetails?> GetPinDetailsAsync(string pin);
    
    /// <summary>
    /// Revokes a PIN
    /// </summary>
    /// <param name="pinCode">The PIN to revoke</param>
    /// <param name="userId">The user requesting revocation</param>
    /// <returns>True if revoked, false if not found</returns>
    Task<bool> RevokePinAsync(string pinCode, string userId);
    
    /// <summary>
    /// Gets active PINs for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of active PINs</returns>
    Task<IEnumerable<ActivePinDto>> GetActivePinsAsync(string userId);
}

/// <summary>
/// Extension methods for ISessionService
/// </summary>
public static class SessionServiceExtensions
{
    /// <summary>
    /// Validates if a session exists and is active
    /// </summary>
    public static async Task<bool> ValidateSessionAsync(this ISessionService sessionService, Guid sessionId)
    {
        try
        {
            // This is a simple validation - in a real implementation, you might want to check
            // if the session exists and is in an active state
            return true; // Placeholder implementation
        }
        catch
        {
            return false;
        }
    }
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
    
    // Multi-monitor support
    Task<IEnumerable<MonitorInfo>> GetMonitorsAsync(string deviceId);
    Task<bool> SelectMonitorAsync(Guid sessionId, string monitorId);
    Task<MonitorInfo?> GetSelectedMonitorAsync(Guid sessionId);
    Task<ScreenBounds?> GetMonitorBoundsAsync(Guid sessionId, string monitorId);
    Task NotifyMonitorChangeAsync(Guid sessionId, string fromMonitorId, string toMonitorId);
    
    // Clipboard operations
    Task<ClipboardContent?> GetClipboardContentAsync(Guid sessionId);
    Task<bool> SetClipboardContentAsync(Guid sessionId, ClipboardContent content);
    Task<ClipboardContent?> GetHostClipboardAsync(Guid sessionId);
    Task<ClipboardContent?> GetClientClipboardAsync(Guid sessionId);
    Task<bool> SetClientClipboardAsync(Guid sessionId, ClipboardContent content);
    Task<ClipboardHistoryItem[]?> GetClipboardHistoryAsync(Guid sessionId, int maxItems);
    Task<bool> ClearClipboardAsync(Guid sessionId, ClipboardTarget target);
    bool IsClipboardTypeSupported(ClipboardContentType type);
}


/// <summary>
/// Interface for adaptive quality service
/// </summary>
public interface IAdaptiveQualityService
{
    Task<QualitySettings> DetermineOptimalQualityAsync(Guid sessionId);
    Task UpdateQualitySettingsAsync(Guid sessionId, QualitySettings settings);
    Task<QualitySettings> GetCurrentQualityAsync(Guid sessionId);
    Task ReportMetricsAsync(Guid sessionId, RemoteC.Shared.Models.SessionMetrics metrics);
    QualitySettings GetQualityPreset(RemoteC.Shared.Models.QualityLevel level);
    Task<bool> ShouldDowngradeQualityAsync(Guid sessionId);
    Task<bool> ShouldUpgradeQualityAsync(Guid sessionId);
}

/// <summary>
/// Interface for session metrics service
/// </summary>
public interface ISessionMetricsService
{
    Task RecordSessionMetricsAsync(Guid sessionId, RemoteC.Shared.Models.SessionMetrics metrics);
    Task<RemoteC.Shared.Models.SessionMetrics?> GetSessionMetricsAsync(Guid sessionId);
    Task<IEnumerable<RemoteC.Shared.Models.SessionMetrics>> GetHistoricalMetricsAsync(Guid sessionId, TimeSpan duration);
    Task ClearMetricsAsync(Guid sessionId);
}

/// <summary>
/// Interface for monitor management operations
/// </summary>
public interface IMonitorService
{
    Task<IEnumerable<MonitorInfo>> GetMonitorsAsync(string deviceId);
    Task<MonitorSelectionResult> SelectMonitorAsync(Guid sessionId, string monitorId);
    Task<MonitorInfo?> GetSelectedMonitorAsync(Guid sessionId);
    Task<VirtualDesktopBounds?> GetVirtualDesktopBoundsAsync(string deviceId);
    Task SwitchMonitorAsync(Guid sessionId, string fromMonitorId, string toMonitorId);
    Task<MonitorInfo?> GetMonitorAtPointAsync(string deviceId, int x, int y);
    Task HandleMonitorConfigurationChangeAsync(string deviceId);
    Task<ScreenBounds?> GetCaptureBoundsAsync(Guid sessionId);
    Task<MonitorInfo?> GetPrimaryMonitorAsync(string deviceId);
    (int X, int Y) ToGlobalCoordinates(MonitorInfo monitor, int localX, int localY);
    (int X, int Y) ToMonitorCoordinates(MonitorInfo monitor, int globalX, int globalY);
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
    Task<RemoteC.Shared.Models.FileTransfer> InitiateTransferAsync(FileTransferRequest request);
    Task<ChunkUploadResult> UploadChunkAsync(FileChunk chunk);
    Task<RemoteC.Shared.Models.FileTransfer?> GetTransferStatusAsync(Guid transferId);
    Task<IEnumerable<int>> GetMissingChunksAsync(Guid transferId);
    Task<FileChunk?> DownloadChunkAsync(Guid transferId, int chunkIndex);
    Task<bool> CancelTransferAsync(Guid transferId);
    Task<FileTransferMetrics> GetTransferMetricsAsync(Guid transferId);
    Task CleanupExpiredTransfersAsync();
    Task<int> CleanupStalledTransfersAsync();
}



