using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services;

public interface ISessionManager
{
    Task<bool> ValidateSessionAsync(SessionStartRequest request);
    Task<SessionInfo> CreateSessionAsync(SessionStartRequest request);
    Task EndSessionAsync(Guid sessionId);
    Task<SessionInfo?> GetSessionAsync(Guid sessionId);
    bool IsSessionActive(Guid sessionId);
    Task UpdateQualitySettingsAsync(Guid sessionId, int quality);
}

public class SessionManager : ISessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ConcurrentDictionary<Guid, SessionInfo> _sessions = new();

    public SessionManager(
        ILogger<SessionManager> logger,
        IAuthenticationService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    public async Task<bool> ValidateSessionAsync(SessionStartRequest request)
    {
        try
        {
            // Validate PIN
            if (!string.IsNullOrEmpty(request.Pin))
            {
                var isPinValid = await _authService.ValidatePinAsync(request.Pin);
                if (!isPinValid)
                {
                    _logger.LogWarning("Invalid PIN for session request: {SessionId}", request.SessionId);
                    return false;
                }
            }

            // Validate user permissions
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var hasPermission = await _authService.CheckPermissionAsync(
                    request.UserId, 
                    "session.create");
                
                if (!hasPermission)
                {
                    _logger.LogWarning("User {UserId} lacks permission for session creation", request.UserId);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session request");
            return false;
        }
    }

    public async Task<SessionInfo> CreateSessionAsync(SessionStartRequest request)
    {
        var session = new SessionInfo
        {
            Id = request.SessionId,
            UserId = request.UserId,
            DeviceId = request.DeviceId,
            Type = request.Type,
            StartTime = DateTime.UtcNow,
            Status = SessionStatus.Active,
            MonitorIndex = request.MonitorIndex,
            QualitySettings = new QualitySettings
            {
                Quality = request.InitialQuality ?? 85,
                TargetFps = request.TargetFps ?? 30
            }
        };

        _sessions[session.Id] = session;
        _logger.LogInformation("Created session {SessionId} for user {UserId}", session.Id, session.UserId);
        
        return await Task.FromResult(session);
    }

    public async Task EndSessionAsync(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.EndTime = DateTime.UtcNow;
            session.Status = SessionStatus.Ended;
            
            _logger.LogInformation("Ended session {SessionId}, duration: {Duration}", 
                sessionId, session.Duration);
        }
        
        await Task.CompletedTask;
    }

    public async Task<SessionInfo?> GetSessionAsync(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return await Task.FromResult(session);
    }

    public bool IsSessionActive(Guid sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) && 
               session.Status == SessionStatus.Active;
    }

    public async Task UpdateQualitySettingsAsync(Guid sessionId, int quality)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.QualitySettings.Quality = quality;
            _logger.LogInformation("Updated quality settings for session {SessionId} to {Quality}", 
                sessionId, quality);
        }
        
        await Task.CompletedTask;
    }
}

public class SessionInfo
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public SessionType Type { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SessionStatus Status { get; set; }
    public int MonitorIndex { get; set; }
    public QualitySettings QualitySettings { get; set; } = new();
    
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    public bool IsTimedOut => Status == SessionStatus.Active && 
                              DateTime.UtcNow - StartTime > TimeSpan.FromHours(8);
}

public class SessionStartRequest
{
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string? Pin { get; set; }
    public SessionType Type { get; set; }
    public int MonitorIndex { get; set; }
    public int? InitialQuality { get; set; }
    public int? TargetFps { get; set; }
}

public class FileTransferRequest
{
    public Guid SessionId { get; set; }
    public FileTransferType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Checksum { get; set; }
}

public enum FileTransferType
{
    Upload,
    Download
}

public class RemoteCommand
{
    public Guid SessionId { get; set; }
    public CommandType Type { get; set; }
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public enum CommandType
{
    PowerShell,
    Cmd,
    Bash,
    Process,
    System
}

public class CommandResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan Duration { get; set; }
}