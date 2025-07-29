using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all sessions for the current user
    /// </summary>
    /// <returns>List of sessions</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions()
    {
        try
        {
            var userId = User.Identity?.Name ?? string.Empty;
            _logger.LogInformation("Getting sessions for user {UserId}", userId);
            
            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific session by ID
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<SessionDto>> GetSession(Guid id)
    {
        try
        {
            var userId = User.Identity?.Name ?? string.Empty;
            _logger.LogInformation("Getting session {SessionId} for user {UserId}", id, userId);

            var session = await _sessionService.GetSessionAsync(id, userId);
            if (session == null)
            {
                return NotFound($"Session {id} not found");
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new remote control session
    /// </summary>
    /// <param name="request">Session creation request</param>
    /// <returns>Created session</returns>
    [HttpPost]
    public async Task<ActionResult<SessionDto>> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? string.Empty;
            _logger.LogInformation("Creating session for device {DeviceId} by user {UserId}", request.DeviceId, userId);

            var session = await _sessionService.CreateSessionAsync(request, userId);
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid session creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Starts a remote control session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session start result</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult<SessionStartResult>> StartSession(Guid id)
    {
        try
        {
            var userId = User.Identity?.Name ?? string.Empty;
            _logger.LogInformation("Starting session {SessionId} for user {UserId}", id, userId);

            var result = await _sessionService.StartSessionAsync(id, userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid session start request for session {SessionId}", id);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized session start attempt for session {SessionId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting session {SessionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Stops a remote control session
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>Session stop result</returns>
    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopSession(Guid id)
    {
        try
        {
            var userId = User.Identity?.Name ?? string.Empty;
            _logger.LogInformation("Stopping session {SessionId} for user {UserId}", id, userId);

            await _sessionService.StopSessionAsync(id, userId);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid session stop request for session {SessionId}", id);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized session stop attempt for session {SessionId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping session {SessionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generates a PIN for device access
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <returns>PIN generation result</returns>
    [HttpPost("{id}/pin")]
    public async Task<ActionResult<PinGenerationResult>> GeneratePin(Guid id)
    {
        try
        {
            var userId = User.Identity?.Name ?? string.Empty;
            _logger.LogInformation("Generating PIN for session {SessionId} by user {UserId}", id, userId);

            var result = await _sessionService.GeneratePinAsync(id, userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid PIN generation request for session {SessionId}", id);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized PIN generation attempt for session {SessionId}", id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PIN for session {SessionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}