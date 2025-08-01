using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using System.Security.Claims;

namespace RemoteC.Api.Controllers
{
    /// <summary>
    /// Controller for PIN-based session operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PinSessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<PinSessionController> _logger;

        public PinSessionController(
            ISessionService sessionService,
            ILogger<PinSessionController> logger)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Join a session using a PIN code (no authentication required)
        /// </summary>
        /// <param name="request">The join request with session ID and PIN</param>
        /// <returns>Session join result with connection details</returns>
        [HttpPost("join")]
        [AllowAnonymous]
        public async Task<ActionResult<SessionJoinResult>> JoinWithPin([FromBody] PinJoinRequest request)
        {
            try
            {
                // For anonymous PIN joins, generate a temporary user ID
                var userId = User.Identity?.IsAuthenticated == true 
                    ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString();

                var result = await _sessionService.JoinSessionWithPinAsync(
                    request.SessionId, 
                    request.Pin, 
                    userId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid session for PIN join");
                return NotFound(new { error = "Session not found" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation for PIN join");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized PIN join attempt");
                return Unauthorized(new { error = "Invalid or expired PIN" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining session with PIN");
                return StatusCode(500, new { error = "An error occurred while joining the session" });
            }
        }

        /// <summary>
        /// Validate a PIN before attempting to join
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="pin">The PIN to validate</param>
        /// <returns>Validation result</returns>
        [HttpGet("validate/{sessionId}/{pin}")]
        [AllowAnonymous]
        public async Task<ActionResult<SessionPinValidationResult>> ValidatePin(Guid sessionId, string pin)
        {
            try
            {
                var isValid = await _sessionService.ValidatePinBeforeJoinAsync(sessionId, pin);
                
                return Ok(new SessionPinValidationResult
                {
                    IsValid = isValid,
                    SessionId = sessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating PIN");
                return StatusCode(500, new { error = "An error occurred while validating the PIN" });
            }
        }

        /// <summary>
        /// Generate a temporary PIN with custom expiration (authenticated)
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="request">PIN generation options</param>
        /// <returns>Generated PIN details</returns>
        [HttpPost("{sessionId}/generate-pin")]
        [Authorize]
        public async Task<ActionResult<ExtendedPinGenerationResult>> GenerateTemporaryPin(
            Guid sessionId, 
            [FromBody] GenerateTemporaryPinRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException("User ID not found");

                var result = await _sessionService.GenerateTemporaryPinAsync(
                    sessionId, 
                    userId, 
                    request.ExpirationMinutes);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid session for PIN generation");
                return NotFound(new { error = "Session not found" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized PIN generation attempt");
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating temporary PIN");
                return StatusCode(500, new { error = "An error occurred while generating the PIN" });
            }
        }
    }

    /// <summary>
    /// Request model for joining a session with PIN
    /// </summary>
    public class PinJoinRequest
    {
        /// <summary>
        /// The session ID to join
        /// </summary>
        public Guid SessionId { get; set; }
        
        /// <summary>
        /// The PIN code
        /// </summary>
        public string Pin { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for generating temporary PIN
    /// </summary>
    public class GenerateTemporaryPinRequest
    {
        /// <summary>
        /// PIN expiration time in minutes (default: 10)
        /// </summary>
        public int ExpirationMinutes { get; set; } = 10;
    }

    /// <summary>
    /// Result of session PIN validation
    /// </summary>
    public class SessionPinValidationResult
    {
        /// <summary>
        /// Whether the PIN is valid
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// The session ID
        /// </summary>
        public Guid SessionId { get; set; }
    }
}