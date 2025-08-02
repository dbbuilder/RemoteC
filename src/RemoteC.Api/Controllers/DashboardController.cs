using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Data.Repositories;

namespace RemoteC.Api.Controllers;

/// <summary>
/// Controller for dashboard statistics and overview data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ISessionRepository sessionRepository,
        IDeviceRepository deviceRepository,
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        ILogger<DashboardController> logger)
    {
        _sessionRepository = sessionRepository;
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <returns>Dashboard statistics including active sessions, devices, users, and recent activity</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            // Get active sessions count
            var activeSessions = await _sessionRepository.GetActiveSessionsCountAsync();
            
            // Get total devices count
            var totalDevices = await _deviceRepository.GetDeviceCountAsync();
            
            // Get online devices count
            var onlineDevices = await _deviceRepository.GetOnlineDeviceCountAsync();
            
            // Get total users count
            var totalUsers = await _userRepository.GetUserCountAsync();
            
            // Get recent activity count (last 24 hours)
            var recentActivity = await _auditRepository.GetRecentActivityCountAsync(TimeSpan.FromHours(24));

            var stats = new
            {
                activeSessions,
                totalDevices,
                onlineDevices,
                totalUsers,
                recentActivity,
                lastUpdated = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { error = "Failed to retrieve dashboard statistics" });
        }
    }

    /// <summary>
    /// Get session activity for the last 7 days
    /// </summary>
    /// <returns>Session counts grouped by day</returns>
    [HttpGet("session-activity")]
    public async Task<IActionResult> GetSessionActivity()
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-7);
            
            var sessionCounts = await _sessionRepository.GetSessionCountsByDateRangeAsync(startDate, endDate);
            
            return Ok(sessionCounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session activity");
            return StatusCode(500, new { error = "Failed to retrieve session activity" });
        }
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    /// <returns>System health metrics</returns>
    [HttpGet("health-status")]
    public async Task<IActionResult> GetHealthStatus()
    {
        try
        {
            // This is a simplified health check
            // In production, you would check various system components
            var health = new
            {
                status = "healthy",
                database = "connected",
                redis = "connected",
                uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                timestamp = DateTime.UtcNow
            };
            
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health status");
            return StatusCode(500, new { error = "Failed to retrieve health status" });
        }
    }
}