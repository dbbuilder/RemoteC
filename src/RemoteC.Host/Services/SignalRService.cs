using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Shared.Models;

namespace RemoteC.Host.Services;

/// <summary>
/// Service for SignalR communication with the server
/// </summary>
public interface ISignalRService
{
    event Func<SessionStartRequest, Task>? OnSessionStartRequested;
    event Func<Guid, Task>? OnSessionEndRequested;
    event Func<InputEventData, Task>? OnInputReceived;
    event Func<RemoteCommand, Task>? OnCommandReceived;
    event Func<FileTransferRequest, Task>? OnFileTransferRequested;
    event Func<Guid, Task>? OnClipboardSyncRequested;
    event Func<Guid, int, Task>? OnQualityChangeRequested;

    Task ConnectAsync(string serverUrl, CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task NotifySessionStartedAsync(Guid sessionId);
    Task NotifySessionEndedAsync(Guid sessionId);
    Task NotifySessionErrorAsync(Guid sessionId, string error);
    Task SendScreenDataAsync(Guid sessionId, ScreenData data);
    Task SendCommandResultAsync(Guid sessionId, CommandResult result);
    Task SendClipboardContentAsync(Guid sessionId, string content);
    Task ReportHealthAsync(HostHealthStatus health);
    bool IsConnected { get; }
}

public class SignalRService : ISignalRService, IAsyncDisposable
{
    private readonly ILogger<SignalRService> _logger;
    private readonly IAuthenticationService _authService;
    private HubConnection? _hubConnection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private CancellationTokenSource? _reconnectCts;
    private string? _currentUrl;

    public event Func<SessionStartRequest, Task>? OnSessionStartRequested;
    public event Func<Guid, Task>? OnSessionEndRequested;
    public event Func<InputEventData, Task>? OnInputReceived;
    public event Func<RemoteCommand, Task>? OnCommandReceived;
    public event Func<FileTransferRequest, Task>? OnFileTransferRequested;
    public event Func<Guid, Task>? OnClipboardSyncRequested;
    public event Func<Guid, int, Task>? OnQualityChangeRequested;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRService(
        ILogger<SignalRService> logger,
        IAuthenticationService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    public async Task ConnectAsync(string serverUrl, CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_hubConnection != null)
            {
                await DisconnectInternalAsync();
            }

            _logger.LogInformation("Connecting to SignalR hub at {ServerUrl}", serverUrl);
            _currentUrl = serverUrl;

            // Get authentication token
            var token = await _authService.GetHostTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Failed to obtain authentication token");
            }

            _logger.LogDebug("Obtained host token successfully");

            // Build connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/hubs/host", options =>
                {
                    options.AccessTokenProvider = async () => 
                    {
                        // Get fresh token if needed
                        var currentToken = await _authService.GetHostTokenAsync();
                        _logger.LogDebug("Providing token for SignalR connection");
                        return currentToken;
                    };
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Register event handlers
            RegisterEventHandlers();

            // Setup reconnection handling
            _hubConnection.Reconnecting += OnReconnecting;
            _hubConnection.Reconnected += OnReconnected;
            _hubConnection.Closed += OnConnectionClosed;

            // Connect
            await _hubConnection.StartAsync(cancellationToken);
            
            // Register host with server
            await RegisterHostAsync();

            _logger.LogInformation("Successfully connected to SignalR hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void RegisterEventHandlers()
    {
        if (_hubConnection == null) return;

        // Session management
        _hubConnection.On<SessionStartRequest>("StartSession", async (request) =>
        {
            _logger.LogInformation("Received session start request: {SessionId}", request.SessionId);
            if (OnSessionStartRequested != null)
                await OnSessionStartRequested(request);
        });

        _hubConnection.On<Guid>("EndSession", async (sessionId) =>
        {
            _logger.LogInformation("Received session end request: {SessionId}", sessionId);
            if (OnSessionEndRequested != null)
                await OnSessionEndRequested(sessionId);
        });

        // Input handling
        _hubConnection.On<InputEventData>("SendInput", async (input) =>
        {
            _logger.LogDebug("Received input event: {Type}", input.Type);
            if (OnInputReceived != null)
                await OnInputReceived(input);
        });

        // Command execution
        _hubConnection.On<RemoteCommand>("ExecuteCommand", async (command) =>
        {
            _logger.LogInformation("Received command: {Type}", command.Type);
            if (OnCommandReceived != null)
                await OnCommandReceived(command);
        });

        // File transfer
        _hubConnection.On<FileTransferRequest>("FileTransfer", async (request) =>
        {
            _logger.LogInformation("Received file transfer request: {Type}", request.Type);
            if (OnFileTransferRequested != null)
                await OnFileTransferRequested(request);
        });

        // Clipboard sync
        _hubConnection.On<Guid>("SyncClipboard", async (sessionId) =>
        {
            _logger.LogDebug("Received clipboard sync request for session: {SessionId}", sessionId);
            if (OnClipboardSyncRequested != null)
                await OnClipboardSyncRequested(sessionId);
        });

        // Quality adjustment
        _hubConnection.On<Guid, int>("ChangeQuality", async (sessionId, quality) =>
        {
            _logger.LogInformation("Received quality change request: {Quality} for session {SessionId}", quality, sessionId);
            if (OnQualityChangeRequested != null)
                await OnQualityChangeRequested(sessionId, quality);
        });
    }

    private async Task RegisterHostAsync()
    {
        if (_hubConnection == null) return;

        var hostInfo = new HostInfo
        {
            HostId = Guid.NewGuid(),
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.ToString(),
            Version = "1.0.0", // TODO: Get from assembly
            Capabilities = new HostCapabilities
            {
                SupportsMultiMonitor = true,
                SupportsFileTransfer = true,
                SupportsClipboard = true,
                SupportsAudio = true,
                SupportsRecording = true,
                MaxSessions = 5
            }
        };

        await _hubConnection.InvokeAsync("RegisterHost", hostInfo);
        _logger.LogInformation("Host registered with server");
    }

    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            await DisconnectInternalAsync();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task DisconnectInternalAsync()
    {
        if (_hubConnection != null)
        {
            _logger.LogInformation("Disconnecting from SignalR hub");
            
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = null;

            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task NotifySessionStartedAsync(Guid sessionId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot notify session started - not connected");
            return;
        }

        await _hubConnection.InvokeAsync("SessionStarted", sessionId);
    }

    public async Task NotifySessionEndedAsync(Guid sessionId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot notify session ended - not connected");
            return;
        }

        await _hubConnection.InvokeAsync("SessionEnded", sessionId);
    }

    public async Task NotifySessionErrorAsync(Guid sessionId, string error)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot notify session error - not connected");
            return;
        }

        await _hubConnection.InvokeAsync("SessionError", sessionId, error);
    }

    public async Task SendScreenDataAsync(Guid sessionId, ScreenData data)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send screen data - not connected");
            return;
        }

        try
        {
            // For large data, consider streaming
            if (data.Data.Length > 1024 * 1024) // 1MB
            {
                await StreamScreenDataAsync(sessionId, data);
            }
            else
            {
                await _hubConnection.InvokeAsync("ScreenData", sessionId, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending screen data for session {SessionId}", sessionId);
        }
    }

    private async Task StreamScreenDataAsync(Guid sessionId, ScreenData data)
    {
        if (_hubConnection == null) return;

        var stream = _hubConnection.StreamAsChannelAsync<byte[]>(
            "StreamScreenData", 
            sessionId, 
            data.Width, 
            data.Height,
            data.Timestamp);

        // TODO: Implement streaming logic
        await Task.CompletedTask;
    }

    public async Task SendCommandResultAsync(Guid sessionId, CommandResult result)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send command result - not connected");
            return;
        }

        await _hubConnection.InvokeAsync("CommandResult", sessionId, result);
    }

    public async Task SendClipboardContentAsync(Guid sessionId, string content)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send clipboard content - not connected");
            return;
        }

        await _hubConnection.InvokeAsync("ClipboardContent", sessionId, content);
    }

    public async Task ReportHealthAsync(HostHealthStatus health)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            return; // Silent fail for health reports
        }

        try
        {
            await _hubConnection.InvokeAsync("ReportHealth", health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting health status");
        }
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR connection lost, attempting to reconnect...");
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR connection restored. ConnectionId: {ConnectionId}", connectionId);
        return RegisterHostAsync();
    }

    private Task OnConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "SignalR connection closed with error");
        }
        else
        {
            _logger.LogInformation("SignalR connection closed");
        }

        // Attempt reconnection if not disposed
        if (_reconnectCts != null && !_reconnectCts.Token.IsCancellationRequested)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    // Need to store the URL separately as HubConnection doesn't expose it
                    await ConnectAsync(_currentUrl ?? "", _reconnectCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect");
                }
            });
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock?.Dispose();
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
    public int ActiveSessions { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime LastReportTime { get; set; }
}