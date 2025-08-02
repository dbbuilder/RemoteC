using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using RemoteC.Shared.Models;
using Serilog;

namespace RemoteC.Api.Hubs;

/// <summary>
/// SignalR hub for real-time remote control session communication
/// </summary>
/// <remarks>
/// The SessionHub manages real-time communication for remote control sessions including:
/// - Mouse and keyboard input streaming
/// - Screen updates
/// - Session status notifications
/// - Chat messages
/// - Control handoffs
/// All methods require authentication. Users are automatically grouped by session ID.
/// </remarks>
[Authorize]
public class SessionHub : Hub
{
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(ILogger<SessionHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when a new connection is established with the hub
    /// </summary>
    /// <returns>A task that represents the asynchronous connect operation</returns>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} connected to SessionHub with connection {ConnectionId}", 
            userId, Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a connection with the hub is terminated
    /// </summary>
    /// <param name="exception">The exception that occurred, if any</param>
    /// <returns>A task that represents the asynchronous disconnect operation</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} disconnected from SessionHub with connection {ConnectionId}. Exception: {Exception}", 
            userId, Context.ConnectionId, exception?.Message);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Joins a session group for real-time updates
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session to join</param>
    /// <returns>A task that represents the asynchronous join operation</returns>
    /// <remarks>
    /// When a user joins a session, they are added to a SignalR group for that session.
    /// All participants in the session will be notified of the new user joining.
    /// </remarks>
    public async Task JoinSession(string sessionId)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} joining session {SessionId}", userId, sessionId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        await Clients.Group($"Session_{sessionId}").SendAsync("UserJoinedSession", userId);
    }

    /// <summary>
    /// Leaves a session group
    /// </summary>
    /// <param name="sessionId">Session ID to leave</param>
    public async Task LeaveSession(string sessionId)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} leaving session {SessionId}", userId, sessionId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        await Clients.Group($"Session_{sessionId}").SendAsync("UserLeftSession", userId);
    }

    /// <summary>
    /// Sends mouse input to session participants
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="input">Mouse input data</param>
    public async Task SendMouseInput(string sessionId, MouseInputDto input)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogDebug("User {UserId} sending mouse input to session {SessionId}", userId, sessionId);
        
        await Clients.OthersInGroup($"Session_{sessionId}").SendAsync("ReceiveMouseInput", input);
    }

    /// <summary>
    /// Sends keyboard input to session participants
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="input">Keyboard input data</param>
    public async Task SendKeyboardInput(string sessionId, KeyboardInputDto input)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogDebug("User {UserId} sending keyboard input to session {SessionId}", userId, sessionId);
        
        await Clients.OthersInGroup($"Session_{sessionId}").SendAsync("ReceiveKeyboardInput", input);
    }

    /// <summary>
    /// Sends session status update to participants
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="status">Session status</param>
    public async Task UpdateSessionStatus(string sessionId, SessionStatus status)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} updating session {SessionId} status to {Status}", 
            userId, sessionId, status);
        
        await Clients.Group($"Session_{sessionId}").SendAsync("SessionStatusChanged", status);
    }

    /// <summary>
    /// Sends screen update notification
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="screenData">Screen data</param>
    public async Task SendScreenUpdate(string sessionId, ScreenUpdateDto screenData)
    {
        await Clients.OthersInGroup($"Session_{sessionId}").SendAsync("ReceiveScreenUpdate", screenData);
    }

    /// <summary>
    /// Sends chat message to session participants
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="message">Chat message</param>
    public async Task SendChatMessage(string sessionId, string message)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        var timestamp = DateTime.UtcNow;
        
        _logger.LogInformation("User {UserId} sending chat message to session {SessionId}", userId, sessionId);
        
        var chatMessage = new ChatMessageDto
        {
            UserId = userId,
            Message = message,
            Timestamp = timestamp
        };
        
        await Clients.Group($"Session_{sessionId}").SendAsync("ReceiveChatMessage", chatMessage);
    }

    /// <summary>
    /// Requests control of the session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    public async Task RequestControl(string sessionId)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} requesting control of session {SessionId}", userId, sessionId);
        
        await Clients.Group($"Session_{sessionId}").SendAsync("ControlRequested", userId);
    }

    /// <summary>
    /// Grants control to a user
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="targetUserId">User to grant control to</param>
    public async Task GrantControl(string sessionId, string targetUserId)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} granting control to {TargetUserId} for session {SessionId}", 
            userId, targetUserId, sessionId);
        
        await Clients.Group($"Session_{sessionId}").SendAsync("ControlGranted", targetUserId);
    }

    /// <summary>
    /// Revokes control from a user
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="targetUserId">User to revoke control from</param>
    public async Task RevokeControl(string sessionId, string targetUserId)
    {
        var userId = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation("User {UserId} revoking control from {TargetUserId} for session {SessionId}", 
            userId, targetUserId, sessionId);
        
        await Clients.Group($"Session_{sessionId}").SendAsync("ControlRevoked", targetUserId);
    }
}