using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using System.Security.Claims;

namespace RemoteC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Validates Azure AD B2C token and creates/updates user
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email {Email}", request.Email);

            // The actual token validation is done by the authentication middleware
            // This endpoint is for creating/updating the user in our database
            
            // Extract claims from the validated token
            var azureId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst("emails")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value ?? request.Email;
            var firstName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "User";
            var lastName = User.FindFirst(ClaimTypes.Surname)?.Value ?? "User";

            if (string.IsNullOrEmpty(azureId))
            {
                return Unauthorized("Invalid token");
            }

            // Create or update user
            var user = await _userService.CreateOrUpdateUserAsync(email, firstName, lastName, azureId);

            var response = new LoginResponse
            {
                Success = true,
                User = user,
                Token = request.Token // Return the same token for now
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// Gets the current user's profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates the current user's profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.UpdateUserAsync(Guid.Parse(userId), request);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the current user's permissions
    /// </summary>
    [HttpGet("permissions")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<string>>> GetPermissions()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var permissions = await _userService.GetUserPermissionsAsync(Guid.Parse(userId));
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validates a session PIN
    /// </summary>
    [HttpPost("validate-pin")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionPinResponse>> ValidatePin([FromBody] SessionPinRequest request)
    {
        try
        {
            _logger.LogInformation("PIN validation attempt for session {SessionId}", request.SessionId);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _sessionService.ValidatePinAsync(request.SessionId, request.Pin);

            if (response.IsValid)
            {
                _logger.LogInformation("PIN validated successfully for session {SessionId}", request.SessionId);
            }
            else
            {
                _logger.LogWarning("Invalid PIN for session {SessionId} from IP {IpAddress}", request.SessionId, ipAddress);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            return StatusCode(500, new SessionPinResponse
            {
                IsValid = false,
                Message = "An error occurred during PIN validation"
            });
        }
    }
}