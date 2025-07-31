using System;
using System.Collections.Concurrent;
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
                        Delta = mouseData.Delta
                    };
                }
                else if (inputData is KeyboardInputData keyData)
                {
                    inputEvent = new KeyboardInputEvent
                    {
                        Action = keyData.Action,
                        KeyCode = keyData.KeyCode,
                        Modifiers = keyData.Modifiers
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
}