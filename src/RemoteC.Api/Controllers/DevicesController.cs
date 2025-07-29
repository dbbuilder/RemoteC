using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Data.Repositories;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    /// Gets all devices for the current user
    /// </summary>
    [HttpGet]
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
    [HttpGet("{id}")]
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
    /// Registers or updates a device
    /// </summary>
    [HttpPost("register")]
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
    /// Updates device status
    /// </summary>
    [HttpPatch("{id}/status")]
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
    /// Deletes a device
    /// </summary>
    [HttpDelete("{id}")]
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

public class RegisterDeviceRequest
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public string? IpAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Version { get; set; }
}

public class UpdateDeviceStatusRequest
{
    public bool IsOnline { get; set; }
    public string? IpAddress { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}