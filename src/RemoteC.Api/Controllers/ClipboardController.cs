using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers
{
    /// <summary>
    /// API controller for clipboard operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClipboardController : ControllerBase
    {
        private readonly ILogger<ClipboardController> _logger;
        private readonly IClipboardService _clipboardService;

        public ClipboardController(
            ILogger<ClipboardController> logger,
            IClipboardService clipboardService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        }

        /// <summary>
        /// Get clipboard content for a session
        /// </summary>
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<ClipboardContent>> GetClipboardContent(Guid sessionId)
        {
            try
            {
                var content = await _clipboardService.GetClipboardContentAsync(sessionId);
                if (content == null)
                {
                    return NoContent();
                }
                return Ok(content);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clipboard content for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Set clipboard content for a session
        /// </summary>
        [HttpPost("session/{sessionId}")]
        public async Task<ActionResult> SetClipboardContent(Guid sessionId, [FromBody] ClipboardContent content)
        {
            try
            {
                var result = await _clipboardService.SetClipboardContentAsync(sessionId, content);
                if (!result)
                {
                    return BadRequest(new { error = "Failed to set clipboard content" });
                }
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting clipboard content for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Sync clipboard between host and client
        /// </summary>
        [HttpPost("session/{sessionId}/sync")]
        public async Task<ActionResult<ClipboardSyncResult>> SyncClipboard(
            Guid sessionId,
            [FromQuery] ClipboardSyncDirection direction = ClipboardSyncDirection.Bidirectional)
        {
            try
            {
                var result = await _clipboardService.SyncClipboardAsync(sessionId, direction);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing clipboard for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Enable clipboard monitoring for automatic sync
        /// </summary>
        [HttpPost("session/{sessionId}/monitoring")]
        public async Task<ActionResult> EnableMonitoring(Guid sessionId, [FromBody] ClipboardMonitoringConfig config)
        {
            try
            {
                var result = await _clipboardService.EnableClipboardMonitoringAsync(sessionId, config);
                if (!result)
                {
                    return BadRequest(new { error = "Failed to enable clipboard monitoring" });
                }
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling clipboard monitoring for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Disable clipboard monitoring
        /// </summary>
        [HttpDelete("session/{sessionId}/monitoring")]
        public async Task<ActionResult> DisableMonitoring(Guid sessionId)
        {
            try
            {
                var result = await _clipboardService.DisableClipboardMonitoringAsync(sessionId);
                if (!result)
                {
                    return BadRequest(new { error = "Failed to disable clipboard monitoring" });
                }
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling clipboard monitoring for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get clipboard history
        /// </summary>
        [HttpGet("session/{sessionId}/history")]
        public async Task<ActionResult<ClipboardHistoryItem[]>> GetClipboardHistory(
            Guid sessionId,
            [FromQuery] int maxItems = 10)
        {
            try
            {
                var history = await _clipboardService.GetClipboardHistoryAsync(sessionId, maxItems);
                return Ok(history);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clipboard history for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Clear clipboard
        /// </summary>
        [HttpDelete("session/{sessionId}")]
        public async Task<ActionResult> ClearClipboard(
            Guid sessionId,
            [FromQuery] ClipboardTarget target = ClipboardTarget.Both)
        {
            try
            {
                var result = await _clipboardService.ClearClipboardAsync(sessionId, target);
                if (!result)
                {
                    return BadRequest(new { error = "Failed to clear clipboard" });
                }
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing clipboard for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Check if content type is supported
        /// </summary>
        [HttpGet("session/{sessionId}/support/{type}")]
        public async Task<ActionResult<bool>> IsContentTypeSupported(Guid sessionId, ClipboardContentType type)
        {
            try
            {
                var supported = await _clipboardService.IsContentTypeSupportedAsync(sessionId, type);
                return Ok(new { supported });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking content type support for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Resolve clipboard conflict
        /// </summary>
        [HttpPost("session/{sessionId}/conflict")]
        public async Task<ActionResult<ClipboardContent>> ResolveConflict(
            Guid sessionId,
            [FromBody] ClipboardConflictRequest request)
        {
            try
            {
                var resolved = await _clipboardService.ResolveClipboardConflictAsync(
                    sessionId,
                    request.HostContent,
                    request.ClientContent,
                    request.Policy);
                return Ok(resolved);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving clipboard conflict for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    /// <summary>
    /// Request model for clipboard conflict resolution
    /// </summary>
    public class ClipboardConflictRequest
    {
        public ClipboardContent HostContent { get; set; } = new();
        public ClipboardContent ClientContent { get; set; } = new();
        public ConflictResolutionPolicy Policy { get; set; } = ConflictResolutionPolicy.PreferNewest;
    }
}