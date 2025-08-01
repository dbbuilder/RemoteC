using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Remote control service implementation that delegates to the configured provider
    /// </summary>
    public class RemoteControlService : IRemoteControlService
    {
        private readonly IRemoteControlProvider _provider;
        private readonly IRemoteControlProviderFactory _providerFactory;
        private readonly ILogger<RemoteControlService> _logger;
        private readonly ConcurrentDictionary<Guid, RemoteSession> _activeSessions;

        public RemoteControlService(
            IRemoteControlProvider provider,
            IRemoteControlProviderFactory providerFactory,
            ILogger<RemoteControlService> logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = new ConcurrentDictionary<Guid, RemoteSession>();
        }

        public async Task<string> StartRemoteSessionAsync(Guid sessionId, string deviceId)
        {
            try
            {
                _logger.LogInformation("Starting remote session {SessionId} for device {DeviceId} using provider: {Provider}", 
                    sessionId, deviceId, _providerFactory.GetCurrentProviderName());

                // Initialize provider if needed
                var initialized = await _provider.InitializeAsync();
                if (!initialized)
                {
                    throw new InvalidOperationException("Failed to initialize remote control provider");
                }

                // Start the session with the provider
                var remoteSession = await _provider.StartSessionAsync(deviceId, sessionId.ToString());
                
                // Store the session
                _activeSessions[sessionId] = remoteSession;
                
                _logger.LogInformation("Remote session {SessionId} started successfully with ID: {RemoteSessionId}", 
                    sessionId, remoteSession.Id);
                
                return remoteSession.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start remote session {SessionId} for device {DeviceId}", 
                    sessionId, deviceId);
                throw;
            }
        }

        public async Task StopRemoteSessionAsync(Guid sessionId)
        {
            try
            {
                _logger.LogInformation("Stopping remote session {SessionId}", sessionId);

                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found in active sessions", sessionId);
                    return;
                }

                // Stop the session with the provider
                var stopped = await _provider.EndSessionAsync(remoteSession.Id);
                
                if (stopped)
                {
                    _activeSessions.TryRemove(sessionId, out _);
                    _logger.LogInformation("Remote session {SessionId} stopped successfully", sessionId);
                }
                else
                {
                    _logger.LogWarning("Provider failed to stop remote session {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop remote session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> SendInputAsync(Guid sessionId, object inputData)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found for input", sessionId);
                    return false;
                }

                // Convert input data to appropriate format
                InputEvent? inputEvent = null;
                
                if (inputData is MouseInputData mouseData)
                {
                    inputEvent = new MouseInputEvent
                    {
                        Action = mouseData.Action,
                        X = mouseData.X,
                        Y = mouseData.Y,
                        Button = mouseData.Button,
                        WheelDelta = mouseData.Delta
                    };
                }
                else if (inputData is KeyboardInputData keyData)
                {
                    inputEvent = new KeyboardInputEvent
                    {
                        Action = (KeyAction)keyData.Action,
                        KeyCode = keyData.KeyCode,
                        CtrlPressed = (keyData.Modifiers & KeyModifiers.Control) != 0,
                        AltPressed = (keyData.Modifiers & KeyModifiers.Alt) != 0,
                        ShiftPressed = (keyData.Modifiers & KeyModifiers.Shift) != 0
                    };
                }
                else
                {
                    _logger.LogWarning("Unknown input data type: {Type}", inputData?.GetType().Name);
                    return false;
                }

                await _provider.SendInputAsync(remoteSession.Id, inputEvent);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send input for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<byte[]> GetScreenshotAsync(Guid sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found for screenshot", sessionId);
                    return Array.Empty<byte>();
                }

                // Capture a single frame
                var frame = await _provider.CaptureScreenAsync(remoteSession.Id);
                
                if (frame == null || frame.Data == null || frame.Data.Length == 0)
                {
                    _logger.LogWarning("No screenshot data received for session {SessionId}", sessionId);
                    return Array.Empty<byte>();
                }

                _logger.LogDebug("Screenshot captured for session {SessionId}: {Width}x{Height}, {Size} bytes", 
                    sessionId, frame.Width, frame.Height, frame.Data.Length);
                
                // Return raw frame data (could be converted to specific image format if needed)
                return frame.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get screenshot for session {SessionId}", sessionId);
                return Array.Empty<byte>();
            }
        }

        public async Task<bool> IsSessionActiveAsync(Guid sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    return false;
                }

                // Check if the provider still considers the session active
                var stats = await _provider.GetStatisticsAsync(remoteSession.Id);
                
                // If we can get statistics, the session is likely active
                return stats != null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking session {SessionId} status, assuming inactive", sessionId);
                return false;
            }
        }

        public async Task<IEnumerable<MonitorInfo>> GetMonitorsAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Getting monitors for device {DeviceId}", deviceId);
                
                // For now, create a temporary session to get monitors
                // In the future, this should be device-based, not session-based
                var tempSession = await _provider.StartSessionAsync(deviceId, Guid.NewGuid().ToString());
                
                try
                {
                    var monitors = await _provider.GetMonitorsAsync(tempSession.Id);
                    return monitors;
                }
                finally
                {
                    // Clean up temporary session
                    await _provider.EndSessionAsync(tempSession.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get monitors for device {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<bool> SelectMonitorAsync(Guid sessionId, string monitorId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found", sessionId);
                    return false;
                }

                _logger.LogInformation("Selecting monitor {MonitorId} for session {SessionId}", monitorId, sessionId);
                return await _provider.SelectMonitorAsync(remoteSession.Id, monitorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select monitor {MonitorId} for session {SessionId}", monitorId, sessionId);
                throw;
            }
        }

        public async Task<MonitorInfo?> GetSelectedMonitorAsync(Guid sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found", sessionId);
                    return null;
                }

                var monitors = await _provider.GetMonitorsAsync(remoteSession.Id);
                // For now, return the first monitor or primary
                return monitors?.FirstOrDefault(m => m.IsPrimary) ?? monitors?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get selected monitor for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<ScreenBounds?> GetMonitorBoundsAsync(Guid sessionId, string monitorId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found", sessionId);
                    return null;
                }

                var monitors = await _provider.GetMonitorsAsync(remoteSession.Id);
                var monitor = monitors?.FirstOrDefault(m => m.Id == monitorId);
                
                if (monitor == null)
                    return null;

                return new ScreenBounds
                {
                    X = monitor.Bounds.X,
                    Y = monitor.Bounds.Y,
                    Width = monitor.Bounds.Width,
                    Height = monitor.Bounds.Height
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get monitor bounds for session {SessionId}, monitor {MonitorId}", sessionId, monitorId);
                return null;
            }
        }

        public async Task NotifyMonitorChangeAsync(Guid sessionId, string fromMonitorId, string toMonitorId)
        {
            try
            {
                _logger.LogInformation("Notifying monitor change for session {SessionId} from {FromMonitor} to {ToMonitor}", 
                    sessionId, fromMonitorId, toMonitorId);
                
                // TODO: Implement SignalR notification to connected clients
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify monitor change for session {SessionId}", sessionId);
                throw;
            }
        }

        // Clipboard operations
        public async Task<ClipboardContent?> GetClipboardContentAsync(Guid sessionId)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found", sessionId);
                    return null;
                }

                // Get clipboard from host via provider
                // For now, return a placeholder implementation
                _logger.LogInformation("Getting clipboard content for session {SessionId}", sessionId);
                
                // TODO: Implement actual clipboard retrieval via provider
                return new ClipboardContent
                {
                    Type = ClipboardContentType.Text,
                    Text = "Clipboard content placeholder",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clipboard content for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> SetClipboardContentAsync(Guid sessionId, ClipboardContent content)
        {
            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var remoteSession))
                {
                    _logger.LogWarning("Remote session {SessionId} not found", sessionId);
                    return false;
                }

                _logger.LogInformation("Setting clipboard content for session {SessionId}, type: {Type}", 
                    sessionId, content.Type);
                
                // TODO: Implement actual clipboard setting via provider
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set clipboard content for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<ClipboardContent?> GetHostClipboardAsync(Guid sessionId)
        {
            // This gets clipboard directly from the host
            return await GetClipboardContentAsync(sessionId);
        }

        public async Task<ClipboardContent?> GetClientClipboardAsync(Guid sessionId)
        {
            // This would get clipboard from the client side
            // For now, return placeholder
            return new ClipboardContent
            {
                Type = ClipboardContentType.Text,
                Text = "Client clipboard placeholder",
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task<bool> SetClientClipboardAsync(Guid sessionId, ClipboardContent content)
        {
            try
            {
                _logger.LogInformation("Setting client clipboard for session {SessionId}", sessionId);
                // TODO: Implement SignalR notification to update client clipboard
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set client clipboard for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<ClipboardHistoryItem[]?> GetClipboardHistoryAsync(Guid sessionId, int maxItems)
        {
            try
            {
                _logger.LogInformation("Getting clipboard history for session {SessionId}, max items: {MaxItems}", 
                    sessionId, maxItems);
                
                // TODO: Implement clipboard history retrieval
                return Array.Empty<ClipboardHistoryItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clipboard history for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<bool> ClearClipboardAsync(Guid sessionId, ClipboardTarget target)
        {
            try
            {
                _logger.LogInformation("Clearing clipboard for session {SessionId}, target: {Target}", 
                    sessionId, target);
                
                // TODO: Implement clipboard clearing
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear clipboard for session {SessionId}", sessionId);
                return false;
            }
        }

        public bool IsClipboardTypeSupported(ClipboardContentType type)
        {
            // Check which clipboard types are supported by the current provider
            return type switch
            {
                ClipboardContentType.Text => true,
                ClipboardContentType.Image => true,
                ClipboardContentType.Html => true,
                ClipboardContentType.FileList => false, // Not supported yet
                ClipboardContentType.RichText => false, // Not supported yet
                _ => false
            };
        }
    }

    /// <summary>
    /// Mouse input data for remote control
    /// </summary>
    public class MouseInputData
    {
        public MouseAction Action { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
        public int Delta { get; set; } // For scroll wheel
    }

    /// <summary>
    /// Keyboard input data for remote control
    /// </summary>
    public class KeyboardInputData
    {
        public KeyboardAction Action { get; set; }
        public int KeyCode { get; set; }
        public KeyModifiers Modifiers { get; set; }
    }

    /// <summary>
    /// Keyboard action types
    /// </summary>
    public enum KeyboardAction
    {
        KeyDown,
        KeyUp,
        KeyPress
    }

    /// <summary>
    /// Keyboard modifier keys
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4,
        Windows = 8
    }
}