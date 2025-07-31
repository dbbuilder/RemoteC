using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers;

/// <summary>
/// Manages PIN-based authentication for quick device access
/// </summary>
/// <remarks>
/// The PinsController provides endpoints for generating and validating PINs
/// that allow quick access to remote sessions without full authentication.
/// PINs are time-limited and can be configured for single or multiple use.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PinsController : ControllerBase
{
    private readonly IPinService _pinService;
    private readonly ILogger<PinsController> _logger;

    public PinsController(IPinService pinService, ILogger<PinsController> logger)
    {
        _pinService = pinService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new PIN for session access
    /// </summary>
    /// <param name="request">PIN generation parameters</param>
    /// <returns>Generated PIN details</returns>
    /// <remarks>
    /// Generates a unique PIN that can be used for quick access to a remote session.
    /// The PIN expires after the specified duration (default 5 minutes).
    /// Requires authentication to generate a PIN.
    /// </remarks>
    /// <response code="200">PIN successfully generated</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PinGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PinGenerationResponse>> GeneratePin([FromBody] PinGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("PIN generation requested for session {SessionId}", request.SessionId);

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _pinService.GeneratePinWithDetailsAsync(request.SessionId, request.ExpirationMinutes);
            
            _logger.LogInformation("PIN generated successfully for session {SessionId}", request.SessionId);
            
            return Ok(new PinGenerationResponse
            {
                Success = true,
                PinCode = result.PinCode,
                SessionId = result.SessionId,
                ExpiresAt = result.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PIN");
            return StatusCode(500, new PinGenerationResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while generating PIN"
            });
        }
    }

    /// <summary>
    /// Validates a PIN (alternative endpoint)
    /// </summary>
    /// <param name="request">PIN validation request</param>
    /// <returns>PIN validation result with session details</returns>
    /// <remarks>
    /// This endpoint provides an alternative to /api/auth/validate-pin.
    /// It's used by host services to validate PINs and get session information.
    /// No authentication required as the PIN itself is the authentication mechanism.
    /// </remarks>
    /// <response code="200">PIN validation result (check IsValid property)</response>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PinValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PinValidationResult>> ValidatePin([FromBody] PinValidateRequest request)
    {
        try
        {
            _logger.LogInformation("PIN validation attempt");

            // Generate session ID if not provided
            var sessionId = request.SessionId ?? Guid.NewGuid();
            
            var isValid = await _pinService.ValidatePinAsync(sessionId, request.Pin);
            
            if (isValid)
            {
                // Get PIN details for the response
                var pinDetails = await _pinService.GetPinDetailsAsync(request.Pin);
                
                _logger.LogInformation("PIN validated successfully for session {SessionId}", sessionId);
                
                return Ok(new PinValidationResult
                {
                    IsValid = true,
                    SessionId = sessionId.ToString(),
                    UserId = pinDetails?.UserId,
                    DeviceId = pinDetails?.DeviceId
                });
            }
            else
            {
                _logger.LogWarning("Invalid PIN validation attempt");
                
                return Ok(new PinValidationResult
                {
                    IsValid = false,
                    Reason = "Invalid or expired PIN"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            return Ok(new PinValidationResult
            {
                IsValid = false,
                Reason = "Validation error occurred"
            });
        }
    }

    /// <summary>
    /// Revokes an active PIN
    /// </summary>
    /// <param name="pinCode">The PIN code to revoke</param>
    /// <returns>Revocation result</returns>
    /// <remarks>
    /// Immediately revokes a PIN, preventing further use.
    /// Only the user who created the PIN or an administrator can revoke it.
    /// </remarks>
    /// <response code="204">PIN successfully revoked</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">PIN not found</response>
    /// <response code="403">User is not authorized to revoke this PIN</response>
    [HttpDelete("{pinCode}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokePin(string pinCode)
    {
        try
        {
            _logger.LogInformation("PIN revocation requested for {PinCode}", pinCode);

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _pinService.RevokePinAsync(pinCode, userId);
            
            if (!result)
            {
                return NotFound("PIN not found or already expired");
            }

            _logger.LogInformation("PIN {PinCode} revoked successfully", pinCode);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You are not authorized to revoke this PIN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking PIN");
            return StatusCode(500, "An error occurred while revoking PIN");
        }
    }

    /// <summary>
    /// Gets active PINs for the current user
    /// </summary>
    /// <returns>List of active PINs</returns>
    /// <remarks>
    /// Returns all active (non-expired) PINs created by the current user.
    /// Does not return the actual PIN codes for security reasons.
    /// </remarks>
    /// <response code="200">List of active PINs</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("active")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<ActivePinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ActivePinDto>>> GetActivePins()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var pins = await _pinService.GetActivePinsAsync(userId);
            
            return Ok(pins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active PINs");
            return StatusCode(500, "An error occurred while retrieving active PINs");
        }
    }
}

/// <summary>
/// Request model for PIN validation (used by host services)
/// </summary>
public class PinValidateRequest
{
    /// <summary>
    /// The PIN code to validate
    /// </summary>
    public string Pin { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional session ID (will be generated if not provided)
    /// </summary>
    public Guid? SessionId { get; set; }
}

/// <summary>
/// Result model for PIN validation (used by host services)
/// </summary>
public class PinValidationResult
{
    /// <summary>
    /// Whether the PIN is valid
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// The user ID associated with the PIN
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// The session ID for the remote session
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// The device ID associated with the PIN
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// Reason for validation failure
    /// </summary>
    public string? Reason { get; set; }
}