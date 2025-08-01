using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service for managing clipboard synchronization
    /// </summary>
    public class ClipboardService : IClipboardService
    {
        private readonly ILogger<ClipboardService> _logger;
        private readonly IRemoteControlService _remoteControlService;
        private readonly ISessionService _sessionService;
        private readonly Dictionary<Guid, ClipboardMonitoringConfig> _monitoringConfigs = new();
        private readonly Dictionary<Guid, List<ClipboardHistoryItem>> _clipboardHistory = new();
        private const long MaxClipboardSize = 10 * 1024 * 1024; // 10MB

        public ClipboardService(
            ILogger<ClipboardService> logger,
            IRemoteControlService remoteControlService,
            ISessionService sessionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _remoteControlService = remoteControlService ?? throw new ArgumentNullException(nameof(remoteControlService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        public async Task<ClipboardContent?> GetClipboardContentAsync(Guid sessionId)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            try
            {
                return await _remoteControlService.GetClipboardContentAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clipboard content for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> SetClipboardContentAsync(Guid sessionId, ClipboardContent content)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            // Validate content size
            if (content.Size > MaxClipboardSize)
            {
                _logger.LogWarning("Clipboard content exceeds maximum size limit for session {SessionId}", sessionId);
                return false;
            }

            try
            {
                var result = await _remoteControlService.SetClipboardContentAsync(sessionId, content);
                
                if (result)
                {
                    // Add to history
                    await AddToHistoryAsync(sessionId, content, ClipboardSource.Client);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting clipboard content for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<ClipboardSyncResult> SyncClipboardAsync(Guid sessionId, ClipboardSyncDirection direction)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            try
            {
                var result = new ClipboardSyncResult
                {
                    Success = false,
                    ActualDirection = ClipboardSyncDirection.None
                };

                if (direction == ClipboardSyncDirection.None)
                {
                    result.ErrorMessage = "No sync direction specified";
                    return result;
                }

                // Get content from both sides if bidirectional
                ClipboardContent? hostContent = null;
                ClipboardContent? clientContent = null;

                if (direction == ClipboardSyncDirection.HostToClient || direction == ClipboardSyncDirection.Bidirectional)
                {
                    hostContent = await _remoteControlService.GetHostClipboardAsync(sessionId);
                }

                if (direction == ClipboardSyncDirection.ClientToHost || direction == ClipboardSyncDirection.Bidirectional)
                {
                    clientContent = await _remoteControlService.GetClientClipboardAsync(sessionId);
                }

                // Determine which content to sync
                if (direction == ClipboardSyncDirection.Bidirectional && hostContent != null && clientContent != null)
                {
                    // Resolve conflict based on timestamp
                    if (clientContent.Timestamp > hostContent.Timestamp)
                    {
                        // Client content is newer
                        await _remoteControlService.SetClipboardContentAsync(sessionId, clientContent);
                        result.SyncedContent = clientContent;
                        result.ActualDirection = ClipboardSyncDirection.ClientToHost;
                    }
                    else
                    {
                        // Host content is newer
                        await _remoteControlService.SetClientClipboardAsync(sessionId, hostContent);
                        result.SyncedContent = hostContent;
                        result.ActualDirection = ClipboardSyncDirection.HostToClient;
                    }
                    result.ConflictResolved = true;
                }
                else if (direction == ClipboardSyncDirection.HostToClient && hostContent != null)
                {
                    await _remoteControlService.SetClientClipboardAsync(sessionId, hostContent);
                    result.SyncedContent = hostContent;
                    result.ActualDirection = ClipboardSyncDirection.HostToClient;
                }
                else if (direction == ClipboardSyncDirection.ClientToHost && clientContent != null)
                {
                    await _remoteControlService.SetClipboardContentAsync(sessionId, clientContent);
                    result.SyncedContent = clientContent;
                    result.ActualDirection = ClipboardSyncDirection.ClientToHost;
                }

                result.Success = result.SyncedContent != null;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing clipboard for session {SessionId}", sessionId);
                return new ClipboardSyncResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> EnableClipboardMonitoringAsync(Guid sessionId, ClipboardMonitoringConfig config)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            try
            {
                _monitoringConfigs[sessionId] = config;
                _logger.LogInformation("Enabled clipboard monitoring for session {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling clipboard monitoring for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> DisableClipboardMonitoringAsync(Guid sessionId)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            try
            {
                _monitoringConfigs.Remove(sessionId);
                _logger.LogInformation("Disabled clipboard monitoring for session {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling clipboard monitoring for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<ClipboardHistoryItem[]> GetClipboardHistoryAsync(Guid sessionId, int maxItems = 10)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            if (_clipboardHistory.TryGetValue(sessionId, out var history))
            {
                return history.OrderByDescending(h => h.Timestamp).Take(maxItems).ToArray();
            }

            // Try to get from remote control service
            return await _remoteControlService.GetClipboardHistoryAsync(sessionId, maxItems) ?? Array.Empty<ClipboardHistoryItem>();
        }

        public async Task<bool> ClearClipboardAsync(Guid sessionId, ClipboardTarget target)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            try
            {
                return await _remoteControlService.ClearClipboardAsync(sessionId, target);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing clipboard for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<bool> IsContentTypeSupportedAsync(Guid sessionId, ClipboardContentType type)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            return _remoteControlService.IsClipboardTypeSupported(type);
        }

        public async Task<ClipboardContent> ResolveClipboardConflictAsync(
            Guid sessionId,
            ClipboardContent hostContent,
            ClipboardContent clientContent,
            ConflictResolutionPolicy policy)
        {
            if (!await _sessionService.ValidateSessionAsync(sessionId))
            {
                throw new InvalidOperationException("Invalid session");
            }

            ClipboardContent resolved;

            switch (policy)
            {
                case ConflictResolutionPolicy.PreferNewest:
                    resolved = hostContent.Timestamp > clientContent.Timestamp ? hostContent : clientContent;
                    break;
                case ConflictResolutionPolicy.PreferHost:
                    resolved = hostContent;
                    break;
                case ConflictResolutionPolicy.PreferClient:
                    resolved = clientContent;
                    break;
                case ConflictResolutionPolicy.Manual:
                    // In manual mode, we'd typically prompt the user
                    // For now, default to newest
                    resolved = hostContent.Timestamp > clientContent.Timestamp ? hostContent : clientContent;
                    break;
                default:
                    resolved = hostContent;
                    break;
            }

            resolved.ResolvedSource = resolved == hostContent ? ClipboardSource.Host : ClipboardSource.Client;
            return resolved;
        }

        private async Task AddToHistoryAsync(Guid sessionId, ClipboardContent content, ClipboardSource source)
        {
            if (!_clipboardHistory.ContainsKey(sessionId))
            {
                _clipboardHistory[sessionId] = new List<ClipboardHistoryItem>();
            }

            var historyItem = new ClipboardHistoryItem
            {
                Content = content,
                Source = source,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            };

            _clipboardHistory[sessionId].Add(historyItem);

            // Keep only last 100 items per session
            if (_clipboardHistory[sessionId].Count > 100)
            {
                _clipboardHistory[sessionId] = _clipboardHistory[sessionId]
                    .OrderByDescending(h => h.Timestamp)
                    .Take(100)
                    .ToList();
            }
        }
    }

    /// <summary>
    /// Interface for clipboard service
    /// </summary>
    public interface IClipboardService
    {
        Task<ClipboardContent?> GetClipboardContentAsync(Guid sessionId);
        Task<bool> SetClipboardContentAsync(Guid sessionId, ClipboardContent content);
        Task<ClipboardSyncResult> SyncClipboardAsync(Guid sessionId, ClipboardSyncDirection direction);
        Task<bool> EnableClipboardMonitoringAsync(Guid sessionId, ClipboardMonitoringConfig config);
        Task<bool> DisableClipboardMonitoringAsync(Guid sessionId);
        Task<ClipboardHistoryItem[]> GetClipboardHistoryAsync(Guid sessionId, int maxItems = 10);
        Task<bool> ClearClipboardAsync(Guid sessionId, ClipboardTarget target);
        Task<bool> IsContentTypeSupportedAsync(Guid sessionId, ClipboardContentType type);
        Task<ClipboardContent> ResolveClipboardConflictAsync(Guid sessionId, ClipboardContent hostContent, ClipboardContent clientContent, ConflictResolutionPolicy policy);
    }
}