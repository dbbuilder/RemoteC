using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Data.Repositories;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers;

/// <summary>
/// Manages device registration, status updates, and device-related operations
/// </summary>
/// <remarks>
/// The DevicesController provides endpoints for device management including:
/// - Device registration and discovery
/// - Status monitoring (online/offline)
/// - Device information retrieval
/// - Device deletion
/// All endpoints require authentication via JWT bearer token.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class DevicesController : ControllerBase
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceRepository deviceRepository, ILogger<DevicesController> logger)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all devices for the current user with pagination support
    /// </summary>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 25, max: 100)</param>
    /// <param name="onlineOnly">Filter to show only online devices (default: false)</param>
    /// <returns>Paginated list of devices</returns>
    /// <response code="200">Returns the paginated list of devices</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DeviceDto>>> GetDevices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool onlineOnly = false)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var devices = await _deviceRepository.GetUserDevicesAsync(userId.Value, pageNumber, pageSize, onlineOnly);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific device by ID
    /// </summary>
    /// <param name="id">The unique identifier of the device</param>
    /// <returns>The device details</returns>
    /// <response code="200">Returns the device details</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Device not found or user doesn't have access</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetDevice(Guid id)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var device = await _deviceRepository.GetDeviceDetailsAsync(id, userId.Value);
            if (device == null)
            {
                return NotFound($"Device {id} not found");
            }

            return Ok(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Registers a new device or updates an existing device
    /// </summary>
    /// <param name="request">Device registration details</param>
    /// <returns>The registered or updated device</returns>
    /// <remarks>
    /// If a device with the same MAC address already exists for the user, it will be updated.
    /// The device will be marked as online upon successful registration.
    /// </remarks>
    /// <response code="200">Device successfully registered or updated</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceDto>> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var device = await _deviceRepository.UpsertDeviceAsync(
                request.Name,
                request.MacAddress,
                userId.Value,
                request.HostName,
                request.IpAddress,
                request.OperatingSystem,
                request.Version,
                true);

            return Ok(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates device online/offline status
    /// </summary>
    /// <param name="id">The unique identifier of the device</param>
    /// <param name="request">Status update details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Status successfully updated</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Device not found or user doesn't have access</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDeviceStatus(Guid id, [FromBody] UpdateDeviceStatusRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Verify user owns the device
            var device = await _deviceRepository.GetDeviceDetailsAsync(id, userId.Value);
            if (device == null)
            {
                return NotFound($"Device {id} not found");
            }

            await _deviceRepository.UpdateDeviceStatusAsync(id, request.IsOnline, request.IpAddress);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device status {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Permanently deletes a device
    /// </summary>
    /// <param name="id">The unique identifier of the device to delete</param>
    /// <returns>No content on success</returns>
    /// <remarks>
    /// This operation is permanent and cannot be undone. All associated sessions and data will be deleted.
    /// </remarks>
    /// <response code="204">Device successfully deleted</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">Device not found or user doesn't have access</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice(Guid id)
    {
        try
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Verify user owns the device
            var device = await _deviceRepository.GetDeviceDetailsAsync(id, userId.Value);
            if (device == null)
            {
                return NotFound($"Device {id} not found");
            }

            await _deviceRepository.DeleteDeviceAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Extracts the user ID from the authentication claims
    /// </summary>
    /// <returns>The user's GUID if found, otherwise null</returns>
    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Request model for device registration
/// </summary>
public class RegisterDeviceRequest
{
    /// <summary>
    /// Friendly name for the device
    /// </summary>
    /// <example>John's Workstation</example>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// MAC address of the device (required for unique identification)
    /// </summary>
    /// <example>00:1B:44:11:3A:B7</example>
    public string MacAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Network hostname of the device
    /// </summary>
    /// <example>DESKTOP-ABC123</example>
    public string? HostName { get; set; }
    
    /// <summary>
    /// Current IP address of the device
    /// </summary>
    /// <example>192.168.1.100</example>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// Operating system name and version
    /// </summary>
    /// <example>Windows 11 Pro</example>
    public string? OperatingSystem { get; set; }
    
    /// <summary>
    /// Agent/client version installed on the device
    /// </summary>
    /// <example>1.0.0</example>
    public string? Version { get; set; }
}

/// <summary>
/// Request model for updating device status
/// </summary>
public class UpdateDeviceStatusRequest
{
    /// <summary>
    /// Indicates whether the device is currently online
    /// </summary>
    public bool IsOnline { get; set; }
    
    /// <summary>
    /// Current IP address of the device (optional)
    /// </summary>
    /// <example>192.168.1.100</example>
    public string? IpAddress { get; set; }
}