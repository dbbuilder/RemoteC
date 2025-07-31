using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers;

/// <summary>
/// Manages permission checks and authorization
/// </summary>
/// <remarks>
/// The PermissionsController provides endpoints for checking user permissions
/// and managing authorization rules. It's primarily used by host services
/// to verify permissions before executing sensitive operations.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PermissionsController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IUserService userService, ILogger<PermissionsController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    /// <param name="request">Permission check request</param>
    /// <returns>Permission check result</returns>
    /// <remarks>
    /// This endpoint is used by host services to verify user permissions
    /// before executing operations. The permission string should follow
    /// the format "Resource.Action" (e.g., "Device.Control", "Session.Create").
    /// Requires authentication as the check is performed on behalf of the authenticated entity.
    /// </remarks>
    /// <response code="200">Permission check result (check HasPermission property)</response>
    /// <response code="401">Caller is not authenticated</response>
    [HttpPost("check")]
    [Authorize]
    [ProducesResponseType(typeof(PermissionCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionCheckResult>> CheckPermission([FromBody] PermissionCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Permission check requested for user {UserId} on {Permission}", 
                request.UserId, request.Permission);

            // Verify the caller is authenticated
            var callerId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(callerId))
            {
                return Unauthorized();
            }

            // Check if the user has the permission
            var permissions = await _userService.GetUserPermissionsAsync(request.UserId);
            var hasPermission = permissions.Contains(request.Permission);

            var result = new PermissionCheckResult
            {
                HasPermission = hasPermission,
                UserId = request.UserId,
                Permission = request.Permission,
                CheckedAt = DateTime.UtcNow
            };

            if (!hasPermission)
            {
                result.Reason = $"User does not have the '{request.Permission}' permission";
                _logger.LogWarning("Permission check failed: User {UserId} lacks {Permission}", 
                    request.UserId, request.Permission);
            }
            else
            {
                _logger.LogInformation("Permission check passed: User {UserId} has {Permission}", 
                    request.UserId, request.Permission);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission");
            return Ok(new PermissionCheckResult
            {
                HasPermission = false,
                UserId = request.UserId,
                Permission = request.Permission,
                Reason = "Error occurred during permission check",
                CheckedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Gets all permissions for a specific user
    /// </summary>
    /// <param name="userId">The user ID to get permissions for</param>
    /// <returns>List of permissions</returns>
    /// <remarks>
    /// Returns all permissions assigned to a user through their roles.
    /// Requires admin privileges to view other users' permissions.
    /// </remarks>
    /// <response code="200">List of permissions</response>
    /// <response code="401">Caller is not authenticated</response>
    /// <response code="403">Caller is not authorized to view these permissions</response>
    [HttpGet("user/{userId}")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserPermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPermissionsResponse>> GetUserPermissions(string userId)
    {
        try
        {
            _logger.LogInformation("Permissions requested for user {UserId}", userId);

            var permissions = await _userService.GetUserPermissionsAsync(userId);
            var user = await _userService.GetUserAsync(userId);

            return Ok(new UserPermissionsResponse
            {
                UserId = userId,
                UserName = user?.Email ?? "Unknown",
                Permissions = permissions.ToList(),
                Roles = user?.Roles?.Select(r => r.Name).ToList() ?? new List<string>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions");
            return StatusCode(500, "An error occurred while retrieving permissions");
        }
    }

    /// <summary>
    /// Gets all available permissions in the system
    /// </summary>
    /// <returns>List of all permissions</returns>
    /// <remarks>
    /// Returns a complete list of all permissions that can be assigned in the system.
    /// Useful for building permission management UIs.
    /// </remarks>
    /// <response code="200">List of all available permissions</response>
    /// <response code="401">Caller is not authenticated</response>
    [HttpGet("available")]
    [Authorize]
    [ProducesResponseType(typeof(AvailablePermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AvailablePermissionsResponse> GetAvailablePermissions()
    {
        try
        {
            // Define all available permissions
            var permissions = new List<PermissionDefinition>
            {
                // Device permissions
                new() { Name = "Device.View", Category = "Device", Description = "View device information" },
                new() { Name = "Device.Control", Category = "Device", Description = "Control remote devices" },
                new() { Name = "Device.Manage", Category = "Device", Description = "Add, edit, or remove devices" },
                
                // Session permissions
                new() { Name = "Session.View", Category = "Session", Description = "View session information" },
                new() { Name = "Session.Create", Category = "Session", Description = "Create new remote sessions" },
                new() { Name = "Session.Join", Category = "Session", Description = "Join existing sessions" },
                new() { Name = "Session.End", Category = "Session", Description = "End active sessions" },
                new() { Name = "Session.Record", Category = "Session", Description = "Record sessions" },
                
                // File permissions
                new() { Name = "File.Upload", Category = "File", Description = "Upload files to remote devices" },
                new() { Name = "File.Download", Category = "File", Description = "Download files from remote devices" },
                new() { Name = "File.Delete", Category = "File", Description = "Delete files on remote devices" },
                
                // Command permissions
                new() { Name = "Command.Execute", Category = "Command", Description = "Execute commands on remote devices" },
                new() { Name = "Command.ViewHistory", Category = "Command", Description = "View command history" },
                
                // User permissions
                new() { Name = "User.View", Category = "User", Description = "View user information" },
                new() { Name = "User.Manage", Category = "User", Description = "Create, edit, or delete users" },
                new() { Name = "User.AssignRoles", Category = "User", Description = "Assign roles to users" },
                
                // Audit permissions
                new() { Name = "Audit.View", Category = "Audit", Description = "View audit logs" },
                new() { Name = "Audit.Export", Category = "Audit", Description = "Export audit logs" },
                
                // System permissions
                new() { Name = "System.Configure", Category = "System", Description = "Configure system settings" },
                new() { Name = "System.ViewMetrics", Category = "System", Description = "View system metrics" }
            };

            return Ok(new AvailablePermissionsResponse
            {
                Permissions = permissions,
                Categories = permissions.Select(p => p.Category).Distinct().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available permissions");
            return StatusCode(500, "An error occurred while retrieving permissions");
        }
    }
}

/// <summary>
/// Request model for permission check
/// </summary>
public class PermissionCheckRequest
{
    /// <summary>
    /// The user ID to check permissions for
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The permission to check (e.g., "Device.Control")
    /// </summary>
    public string Permission { get; set; } = string.Empty;
}

/// <summary>
/// Result model for permission check
/// </summary>
public class PermissionCheckResult
{
    /// <summary>
    /// Whether the user has the permission
    /// </summary>
    public bool HasPermission { get; set; }
    
    /// <summary>
    /// The user ID that was checked
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The permission that was checked
    /// </summary>
    public string Permission { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for denial (if applicable)
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When the check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Response model for user permissions
/// </summary>
public class UserPermissionsResponse
{
    /// <summary>
    /// The user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's display name or email
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// List of permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    
    /// <summary>
    /// List of roles assigned to the user
    /// </summary>
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Response model for available permissions
/// </summary>
public class AvailablePermissionsResponse
{
    /// <summary>
    /// List of all available permissions
    /// </summary>
    public List<PermissionDefinition> Permissions { get; set; } = new();
    
    /// <summary>
    /// List of permission categories
    /// </summary>
    public List<string> Categories { get; set; } = new();
}

/// <summary>
/// Definition of a permission
/// </summary>
public class PermissionDefinition
{
    /// <summary>
    /// The permission name (e.g., "Device.Control")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The permission category
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what the permission allows
    /// </summary>
    public string Description { get; set; } = string.Empty;
}