using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using System.Security.Claims;

namespace RemoteC.Api.Controllers
{
    /// <summary>
    /// Controller for multi-monitor operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _monitorService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<MonitorController> _logger;

        public MonitorController(
            IMonitorService monitorService,
            ISessionService sessionService,
            ILogger<MonitorController> logger)
        {
            _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all available monitors for a device
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <returns>List of available monitors</returns>
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<IEnumerable<MonitorInfo>>> GetMonitors(string deviceId)
        {
            try
            {
                var monitors = await _monitorService.GetMonitorsAsync(deviceId);
                return Ok(monitors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitors for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to retrieve monitors" });
            }
        }

        /// <summary>
        /// Select a monitor for a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="request">Monitor selection request</param>
        /// <returns>Selection result</returns>
        [HttpPost("session/{sessionId}/select")]
        public async Task<ActionResult<MonitorSelectionResult>> SelectMonitor(
            Guid sessionId, 
            [FromBody] SelectMonitorRequest request)
        {
            try
            {
                // Verify user has access to session
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var session = await _sessionService.GetSessionAsync(sessionId, userId ?? "");
                
                if (session == null)
                {
                    return NotFound(new { error = "Session not found or access denied" });
                }

                var result = await _monitorService.SelectMonitorAsync(sessionId, request.MonitorId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting monitor for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to select monitor" });
            }
        }

        /// <summary>
        /// Get the currently selected monitor for a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Currently selected monitor</returns>
        [HttpGet("session/{sessionId}/selected")]
        public async Task<ActionResult<MonitorInfo>> GetSelectedMonitor(Guid sessionId)
        {
            try
            {
                var monitor = await _monitorService.GetSelectedMonitorAsync(sessionId);
                
                if (monitor == null)
                {
                    return NotFound(new { error = "No monitor selected for session" });
                }
                
                return Ok(monitor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected monitor for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to get selected monitor" });
            }
        }

        /// <summary>
        /// Get virtual desktop bounds encompassing all monitors
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <returns>Virtual desktop bounds</returns>
        [HttpGet("device/{deviceId}/virtual-bounds")]
        public async Task<ActionResult<VirtualDesktopBounds>> GetVirtualDesktopBounds(string deviceId)
        {
            try
            {
                var bounds = await _monitorService.GetVirtualDesktopBoundsAsync(deviceId);
                
                if (bounds == null)
                {
                    return NotFound(new { error = "No monitors found for device" });
                }
                
                return Ok(bounds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual desktop bounds for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get virtual desktop bounds" });
            }
        }

        /// <summary>
        /// Get the primary monitor for a device
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <returns>Primary monitor information</returns>
        [HttpGet("device/{deviceId}/primary")]
        public async Task<ActionResult<MonitorInfo>> GetPrimaryMonitor(string deviceId)
        {
            try
            {
                var monitor = await _monitorService.GetPrimaryMonitorAsync(deviceId);
                
                if (monitor == null)
                {
                    return NotFound(new { error = "No primary monitor found" });
                }
                
                return Ok(monitor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary monitor for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to get primary monitor" });
            }
        }

        /// <summary>
        /// Get monitor at specific coordinates
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Monitor at the specified point</returns>
        [HttpGet("device/{deviceId}/at-point")]
        public async Task<ActionResult<MonitorInfo>> GetMonitorAtPoint(
            string deviceId, 
            [FromQuery] int x, 
            [FromQuery] int y)
        {
            try
            {
                var monitor = await _monitorService.GetMonitorAtPointAsync(deviceId, x, y);
                
                if (monitor == null)
                {
                    return NotFound(new { error = "No monitor found at specified coordinates" });
                }
                
                return Ok(monitor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitor at point ({X}, {Y}) for device {DeviceId}", 
                    x, y, deviceId);
                return StatusCode(500, new { error = "Failed to get monitor at point" });
            }
        }

        /// <summary>
        /// Handle monitor configuration change notification
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <returns>Success status</returns>
        [HttpPost("device/{deviceId}/configuration-changed")]
        public async Task<IActionResult> HandleConfigurationChange(string deviceId)
        {
            try
            {
                await _monitorService.HandleMonitorConfigurationChangeAsync(deviceId);
                return Ok(new { message = "Configuration change handled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling monitor configuration change for device {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Failed to handle configuration change" });
            }
        }

        /// <summary>
        /// Get capture bounds for current session monitor selection
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Capture bounds</returns>
        [HttpGet("session/{sessionId}/capture-bounds")]
        public async Task<ActionResult<ScreenBounds>> GetCaptureBounds(Guid sessionId)
        {
            try
            {
                var bounds = await _monitorService.GetCaptureBoundsAsync(sessionId);
                
                if (bounds == null)
                {
                    return NotFound(new { error = "No capture bounds available for session" });
                }
                
                return Ok(bounds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting capture bounds for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to get capture bounds" });
            }
        }

        /// <summary>
        /// Get monitors for a device (simplified endpoint)
        /// </summary>
        [HttpGet("{deviceId}")]
        public async Task<ActionResult<IEnumerable<MonitorInfo>>> GetMonitorsSimplified(string deviceId)
        {
            return await GetMonitors(deviceId);
        }

        /// <summary>
        /// Get virtual desktop bounds (simplified endpoint)
        /// </summary>
        [HttpGet("{deviceId}/virtual-desktop")]
        public async Task<ActionResult<VirtualDesktopBounds>> GetVirtualDesktopSimplified(string deviceId)
        {
            return await GetVirtualDesktopBounds(deviceId);
        }
    }

    /// <summary>
    /// Request to select a monitor
    /// </summary>
    public class SelectMonitorRequest
    {
        /// <summary>
        /// Monitor identifier to select
        /// </summary>
        public string MonitorId { get; set; } = string.Empty;
    }
}