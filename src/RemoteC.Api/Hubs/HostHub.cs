using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Hubs;

/// <summary>
/// SignalR hub for host machine connections
/// </summary>
[Authorize]
public class HostHub : Hub
{
    private readonly ILogger<HostHub> _logger;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public HostHub(
        ILogger<HostHub> logger,
        ISessionService sessionService,
        IAuditService auditService,
        IUserService userService)
    {
        _logger = logger;
        _sessionService = sessionService;
        _auditService = auditService;
        _userService = userService;
    }

    public override async Task OnConnectedAsync()
    {
        var hostId = Context.UserIdentifier;
        _logger.LogInformation("Host connected: {HostId} ({ConnectionId})", hostId, Context.ConnectionId);
        
        // Add host to a group based on their ID
        if (!string.IsNullOrEmpty(hostId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"host-{hostId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var hostId = Context.UserIdentifier;
        _logger.LogInformation("Host disconnected: {HostId} ({ConnectionId})", hostId, Context.ConnectionId);
        
        if (!string.IsNullOrEmpty(hostId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"host-{hostId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called when a host notifies that a session has started
    /// </summary>
    public async Task SessionStarted(Guid sessionId)
    {
        _logger.LogInformation("Session started notification from host: {SessionId}", sessionId);
        
        // Update session status
        await _sessionService.UpdateSessionStatusAsync(sessionId, SessionStatus.Active);
        
        // Notify viewers in the session
        await Clients.Group($"session-{sessionId}").SendAsync("SessionStarted", sessionId);
    }

    /// <summary>
    /// Called when a host notifies that a session has ended
    /// </summary>
    public async Task SessionEnded(Guid sessionId)
    {
        _logger.LogInformation("Session ended notification from host: {SessionId}", sessionId);
        
        // Update session status
        await _sessionService.UpdateSessionStatusAsync(sessionId, SessionStatus.Ended);
        
        // Notify viewers in the session
        await Clients.Group($"session-{sessionId}").SendAsync("SessionEnded", sessionId);
    }

    /// <summary>
    /// Called when a host reports an error in a session
    /// </summary>
    public async Task SessionError(Guid sessionId, string error)
    {
        _logger.LogError("Session error from host: {SessionId} - {Error}", sessionId, error);
        
        // Update session status
        await _sessionService.UpdateSessionStatusAsync(sessionId, SessionStatus.Error);
        
        // Notify viewers in the session
        await Clients.Group($"session-{sessionId}").SendAsync("SessionError", sessionId, error);
    }

    /// <summary>
    /// Called when a host sends screen data
    /// </summary>
    public async Task ScreenData(Guid sessionId, ScreenData data)
    {
        // Forward screen data to viewers in the session
        await Clients.Group($"session-{sessionId}").SendAsync("ReceiveScreenData", sessionId, data);
    }

    /// <summary>
    /// Called when a host sends a command result
    /// </summary>
    public async Task CommandResult(Guid sessionId, CommandResult result)
    {
        _logger.LogDebug("Command result from host: {SessionId} - {CommandId}", sessionId, result.CommandId);
        
        // Forward to viewers
        await Clients.Group($"session-{sessionId}").SendAsync("ReceiveCommandResult", sessionId, result);
    }

    /// <summary>
    /// Called when a host sends clipboard content
    /// </summary>
    public async Task ClipboardContent(Guid sessionId, string content)
    {
        _logger.LogDebug("Clipboard content from host for session: {SessionId}", sessionId);
        
        // Forward to viewers
        await Clients.Group($"session-{sessionId}").SendAsync("ReceiveClipboardContent", sessionId, content);
    }

    /// <summary>
    /// Called when a host registers itself with the server
    /// </summary>
    public async Task RegisterHost(HostInfo hostInfo)
    {
        var hostId = Context.UserIdentifier;
        _logger.LogInformation("Host registering: {HostId} - Machine: {MachineName}, OS: {OS}, Version: {Version}", 
            hostId, hostInfo.MachineName, hostInfo.OperatingSystem, hostInfo.Version);
        
        // Store host information (could be in cache or database)
        // For now, just log and notify
        
        // Add to host group
        if (!string.IsNullOrEmpty(hostId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"host-{hostId}");
        }
        
        // Notify administrators about new host
        await Clients.Group("administrators").SendAsync("HostRegistered", hostId, hostInfo);
    }

    /// <summary>
    /// Called when a host reports its health status
    /// </summary>
    public async Task ReportHealth(HostHealthStatus health)
    {
        var hostId = Context.UserIdentifier;
        _logger.LogDebug("Health report from host {HostId}: CPU={Cpu}%, Memory={Memory}%, Disk={Disk}%", 
            hostId, health.CpuUsage, health.MemoryUsage, health.DiskUsage);
        
        // Could store this in a cache or database for monitoring
        // For now, just log it
    }
}

public class HostInfo
{
    public Guid HostId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public HostCapabilities Capabilities { get; set; } = new();
}

public class HostCapabilities
{
    public bool SupportsMultiMonitor { get; set; }
    public bool SupportsFileTransfer { get; set; }
    public bool SupportsClipboard { get; set; }
    public bool SupportsAudio { get; set; }
    public bool SupportsRecording { get; set; }
    public int MaxSessions { get; set; }
}

public class HostHealthStatus
{
    public bool IsHealthy { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public int ActiveSessions { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime LastReportTime { get; set; }
}