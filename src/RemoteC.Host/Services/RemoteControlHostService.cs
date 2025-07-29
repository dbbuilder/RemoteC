using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteC.Host.Services;

/// <summary>
/// Main background service that manages remote control functionality
/// </summary>
public class RemoteControlHostService : BackgroundService
{
    private readonly ILogger<RemoteControlHostService> _logger;
    private readonly ISignalRService _signalRService;
    private readonly IScreenCaptureService _screenCapture;
    private readonly IInputControlService _inputControl;
    private readonly ISessionManager _sessionManager;
    private readonly IConnectionManager _connectionManager;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IPerformanceMonitorService _performanceMonitor;
    private readonly HostConfiguration _config;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeSessions;

    public RemoteControlHostService(
        ILogger<RemoteControlHostService> logger,
        ISignalRService signalRService,
        IScreenCaptureService screenCapture,
        IInputControlService inputControl,
        ISessionManager sessionManager,
        IConnectionManager connectionManager,
        ICommandExecutor commandExecutor,
        IPerformanceMonitorService performanceMonitor,
        IOptions<HostConfiguration> config)
    {
        _logger = logger;
        _signalRService = signalRService;
        _screenCapture = screenCapture;
        _inputControl = inputControl;
        _sessionManager = sessionManager;
        _connectionManager = connectionManager;
        _commandExecutor = commandExecutor;
        _performanceMonitor = performanceMonitor;
        _config = config.Value;
        _activeSessions = new ConcurrentDictionary<Guid, CancellationTokenSource>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("RemoteC Host Service starting...");

            // Initialize services
            await InitializeServicesAsync(stoppingToken);

            // Connect to server
            await _signalRService.ConnectAsync(_config.ServerUrl, stoppingToken);

            // Register event handlers
            RegisterEventHandlers();

            // Main service loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Report health status
                    await ReportHealthStatusAsync();

                    // Process pending commands
                    await ProcessPendingCommandsAsync(stoppingToken);

                    // Update active sessions
                    await UpdateActiveSessionsAsync(stoppingToken);

                    // Wait before next iteration
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in service loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in RemoteControlHostService");
            throw;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task InitializeServicesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing RemoteC Host services...");

        // Initialize screen capture
        await _screenCapture.InitializeAsync(cancellationToken);

        // Initialize input control
        await _inputControl.InitializeAsync(cancellationToken);

        // Initialize performance monitoring
        await _performanceMonitor.StartMonitoringAsync(cancellationToken);

        _logger.LogInformation("RemoteC Host services initialized successfully");
    }

    private void RegisterEventHandlers()
    {
        // Session events
        _signalRService.OnSessionStartRequested += OnSessionStartRequested;
        _signalRService.OnSessionEndRequested += OnSessionEndRequested;
        _signalRService.OnInputReceived += OnInputReceived;
        _signalRService.OnCommandReceived += OnCommandReceived;
        _signalRService.OnFileTransferRequested += OnFileTransferRequested;
        _signalRService.OnClipboardSyncRequested += OnClipboardSyncRequested;
        _signalRService.OnQualityChangeRequested += OnQualityChangeRequested;
    }

    private async Task OnSessionStartRequested(SessionStartRequest request)
    {
        try
        {
            _logger.LogInformation("Session start requested: {SessionId}", request.SessionId);

            // Validate request
            if (!await _sessionManager.ValidateSessionAsync(request))
            {
                _logger.LogWarning("Invalid session request: {SessionId}", request.SessionId);
                return;
            }

            // Create session
            var session = await _sessionManager.CreateSessionAsync(request);
            
            // Start session worker
            var cts = new CancellationTokenSource();
            if (_activeSessions.TryAdd(session.Id, cts))
            {
                _ = Task.Run(async () => await RunSessionAsync(session, cts.Token));
                
                // Notify server of session start
                await _signalRService.NotifySessionStartedAsync(session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session: {SessionId}", request.SessionId);
            await _signalRService.NotifySessionErrorAsync(request.SessionId, ex.Message);
        }
    }

    private async Task OnSessionEndRequested(Guid sessionId)
    {
        try
        {
            _logger.LogInformation("Session end requested: {SessionId}", sessionId);

            if (_activeSessions.TryRemove(sessionId, out var cts))
            {
                cts.Cancel();
                
                // Clean up session
                await _sessionManager.EndSessionAsync(sessionId);
                
                // Notify server
                await _signalRService.NotifySessionEndedAsync(sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session: {SessionId}", sessionId);
        }
    }

    private async Task OnInputReceived(InputEventData input)
    {
        try
        {
            // Validate session
            if (!_sessionManager.IsSessionActive(input.SessionId))
            {
                _logger.LogWarning("Input received for inactive session: {SessionId}", input.SessionId);
                return;
            }

            // Apply input based on type
            switch (input.Type)
            {
                case InputType.MouseMove:
                    await _inputControl.MoveMouseAsync(input.X, input.Y);
                    break;
                case InputType.MouseClick:
                    await _inputControl.MouseClickAsync(input.Button, input.X, input.Y);
                    break;
                case InputType.MouseWheel:
                    await _inputControl.MouseWheelAsync(input.Delta);
                    break;
                case InputType.KeyPress:
                    await _inputControl.KeyPressAsync(input.Key, input.Modifiers);
                    break;
                case InputType.KeyDown:
                    await _inputControl.KeyDownAsync(input.Key, input.Modifiers);
                    break;
                case InputType.KeyUp:
                    await _inputControl.KeyUpAsync(input.Key, input.Modifiers);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input event");
        }
    }

    private async Task OnCommandReceived(RemoteCommand command)
    {
        try
        {
            _logger.LogInformation("Command received: {CommandType} for session {SessionId}", 
                command.Type, command.SessionId);

            // Validate session
            if (!_sessionManager.IsSessionActive(command.SessionId))
            {
                _logger.LogWarning("Command received for inactive session: {SessionId}", command.SessionId);
                return;
            }

            // Execute command
            var result = await _commandExecutor.ExecuteCommandAsync(command);
            
            // Send result back
            await _signalRService.SendCommandResultAsync(command.SessionId, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command");
            await _signalRService.SendCommandResultAsync(command.SessionId, 
                new CommandResult { Success = false, Error = ex.Message });
        }
    }

    private async Task OnFileTransferRequested(FileTransferRequest request)
    {
        try
        {
            _logger.LogInformation("File transfer requested: {Type} for {Path}", 
                request.Type, request.Path);

            // Implement file transfer logic
            // This would handle upload/download operations
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file transfer");
        }
    }

    private async Task OnClipboardSyncRequested(Guid sessionId)
    {
        try
        {
            // Get clipboard content
            var content = await _inputControl.GetClipboardContentAsync();
            
            // Send to server
            await _signalRService.SendClipboardContentAsync(sessionId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing clipboard");
        }
    }

    private async Task OnQualityChangeRequested(Guid sessionId, int quality)
    {
        try
        {
            // Update quality settings for session
            await _sessionManager.UpdateQualitySettingsAsync(sessionId, quality);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing quality settings");
        }
    }

    private async Task RunSessionAsync(SessionInfo session, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting session worker for {SessionId}", session.Id);

            // Get quality settings
            var quality = session.QualitySettings ?? new QualitySettings();
            
            // Calculate frame interval based on FPS
            var frameInterval = TimeSpan.FromMilliseconds(1000.0 / quality.TargetFps);
            var lastFrameTime = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Capture screen
                    var screenData = await _screenCapture.CaptureScreenAsync(
                        session.MonitorIndex, 
                        quality, 
                        cancellationToken);

                    if (screenData != null)
                    {
                        // Send screen data
                        await _signalRService.SendScreenDataAsync(session.Id, screenData);
                        
                        // Update metrics
                        _performanceMonitor.RecordFrameSent(session.Id, screenData.Data.Length);
                    }

                    // Calculate next frame time
                    var now = DateTime.UtcNow;
                    var elapsed = now - lastFrameTime;
                    var delay = frameInterval - elapsed;
                    
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    
                    lastFrameTime = DateTime.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in session worker for {SessionId}", session.Id);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }

            _logger.LogInformation("Session worker ended for {SessionId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in session worker for {SessionId}", session.Id);
        }
    }

    private async Task ReportHealthStatusAsync()
    {
        try
        {
            var health = new HostHealthStatus
            {
                IsHealthy = true,
                CpuUsage = await _performanceMonitor.GetCpuUsageAsync(),
                MemoryUsage = await _performanceMonitor.GetMemoryUsageAsync(),
                ActiveSessions = _activeSessions.Count,
                Uptime = _performanceMonitor.GetUptime(),
                LastReportTime = DateTime.UtcNow
            };

            await _signalRService.ReportHealthAsync(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting health status");
        }
    }

    private async Task ProcessPendingCommandsAsync(CancellationToken cancellationToken)
    {
        // Process any queued commands
        await _commandExecutor.ProcessPendingCommandsAsync(cancellationToken);
    }

    private async Task UpdateActiveSessionsAsync(CancellationToken cancellationToken)
    {
        // Update session metrics and handle timeouts
        foreach (var sessionId in _activeSessions.Keys)
        {
            try
            {
                var session = await _sessionManager.GetSessionAsync(sessionId);
                if (session != null && session.IsTimedOut)
                {
                    _logger.LogWarning("Session {SessionId} timed out", sessionId);
                    await OnSessionEndRequested(sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session {SessionId}", sessionId);
            }
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up RemoteC Host Service...");

            // End all active sessions
            foreach (var kvp in _activeSessions)
            {
                kvp.Value.Cancel();
            }

            // Wait for sessions to end
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Disconnect from server
            await _signalRService.DisconnectAsync();

            // Stop services
            await _performanceMonitor.StopMonitoringAsync();
            await _screenCapture.DisposeAsync();
            await _inputControl.DisposeAsync();

            _logger.LogInformation("RemoteC Host Service cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }
}