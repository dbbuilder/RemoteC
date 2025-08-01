using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service for managing multi-monitor functionality
    /// </summary>
    public class MonitorService : IMonitorService
    {
        private readonly IRemoteControlService _remoteControlService;
        private readonly ILogger<MonitorService> _logger;
        
        // Track selected monitors per session
        private readonly Dictionary<Guid, string> _sessionMonitorMap = new();

        public MonitorService(
            IRemoteControlService remoteControlService,
            ILogger<MonitorService> logger)
        {
            _remoteControlService = remoteControlService ?? throw new ArgumentNullException(nameof(remoteControlService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<MonitorInfo>> GetMonitorsAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Getting monitors for device {DeviceId}", deviceId);
                var monitors = await _remoteControlService.GetMonitorsAsync(deviceId);
                
                _logger.LogInformation("Found {Count} monitors for device {DeviceId}", 
                    monitors?.Count() ?? 0, deviceId);
                
                return monitors ?? Enumerable.Empty<MonitorInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitors for device {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<MonitorSelectionResult> SelectMonitorAsync(Guid sessionId, string monitorId)
        {
            try
            {
                _logger.LogInformation("Selecting monitor {MonitorId} for session {SessionId}", 
                    monitorId, sessionId);

                var success = await _remoteControlService.SelectMonitorAsync(sessionId, monitorId);
                
                if (success)
                {
                    _sessionMonitorMap[sessionId] = monitorId;
                    
                    return new MonitorSelectionResult
                    {
                        Success = true,
                        SelectedMonitorId = monitorId
                    };
                }
                
                return new MonitorSelectionResult
                {
                    Success = false,
                    ErrorMessage = $"Monitor {monitorId} not found or unavailable"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting monitor {MonitorId} for session {SessionId}", 
                    monitorId, sessionId);
                
                return new MonitorSelectionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MonitorInfo?> GetSelectedMonitorAsync(Guid sessionId)
        {
            try
            {
                if (_sessionMonitorMap.TryGetValue(sessionId, out var monitorId))
                {
                    return await _remoteControlService.GetSelectedMonitorAsync(sessionId);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected monitor for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<VirtualDesktopBounds?> GetVirtualDesktopBoundsAsync(string deviceId)
        {
            try
            {
                var monitors = await GetMonitorsAsync(deviceId);
                if (!monitors.Any())
                {
                    return null;
                }

                var minX = monitors.Min(m => m.Bounds.X);
                var minY = monitors.Min(m => m.Bounds.Y);
                var maxX = monitors.Max(m => m.Bounds.Right);
                var maxY = monitors.Max(m => m.Bounds.Bottom);

                return new VirtualDesktopBounds
                {
                    X = minX,
                    Y = minY,
                    Width = maxX - minX,
                    Height = maxY - minY,
                    MonitorCount = monitors.Count()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating virtual desktop bounds for device {DeviceId}", deviceId);
                return null;
            }
        }

        public async Task SwitchMonitorAsync(Guid sessionId, string fromMonitorId, string toMonitorId)
        {
            try
            {
                _logger.LogInformation("Switching monitor from {FromMonitor} to {ToMonitor} for session {SessionId}", 
                    fromMonitorId, toMonitorId, sessionId);

                await SelectMonitorAsync(sessionId, toMonitorId);
                await _remoteControlService.NotifyMonitorChangeAsync(sessionId, fromMonitorId, toMonitorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching monitors for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<MonitorInfo?> GetMonitorAtPointAsync(string deviceId, int x, int y)
        {
            try
            {
                var monitors = await GetMonitorsAsync(deviceId);
                return monitors.FirstOrDefault(m => m.Bounds.Contains(x, y));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding monitor at point ({X}, {Y}) for device {DeviceId}", 
                    x, y, deviceId);
                return null;
            }
        }

        public async Task HandleMonitorConfigurationChangeAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("Handling monitor configuration change for device {DeviceId}", deviceId);
                
                // Refresh monitor list
                var monitors = await GetMonitorsAsync(deviceId);
                
                // Check if any active sessions are using disconnected monitors
                var activeSessions = _sessionMonitorMap.ToList();
                foreach (var (sessionId, monitorId) in activeSessions)
                {
                    var monitorExists = monitors.Any(m => m.Id == monitorId);
                    if (!monitorExists)
                    {
                        _logger.LogWarning("Monitor {MonitorId} no longer exists, switching session {SessionId} to primary", 
                            monitorId, sessionId);
                        
                        // Switch to primary monitor
                        var primary = await GetPrimaryMonitorAsync(deviceId);
                        if (primary != null)
                        {
                            await SelectMonitorAsync(sessionId, primary.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling monitor configuration change for device {DeviceId}", deviceId);
            }
        }

        public async Task<ScreenBounds?> GetCaptureBoundsAsync(Guid sessionId)
        {
            try
            {
                if (_sessionMonitorMap.TryGetValue(sessionId, out var monitorId))
                {
                    var bounds = await _remoteControlService.GetMonitorBoundsAsync(sessionId, monitorId);
                    return bounds;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting capture bounds for session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<MonitorInfo?> GetPrimaryMonitorAsync(string deviceId)
        {
            try
            {
                var monitors = await GetMonitorsAsync(deviceId);
                return monitors.FirstOrDefault(m => m.IsPrimary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary monitor for device {DeviceId}", deviceId);
                return null;
            }
        }

        public (int X, int Y) ToGlobalCoordinates(MonitorInfo monitor, int localX, int localY)
        {
            return (monitor.Bounds.X + localX, monitor.Bounds.Y + localY);
        }

        public (int X, int Y) ToMonitorCoordinates(MonitorInfo monitor, int globalX, int globalY)
        {
            return (globalX - monitor.Bounds.X, globalY - monitor.Bounds.Y);
        }
    }
}