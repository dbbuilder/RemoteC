using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemoteC.Api.Services;
using RemoteC.Shared.Models;
using System.Security.Claims;

namespace RemoteC.Api.Controllers;

/// <summary>
/// Handles authentication, authorization, and user profile management
/// </summary>
/// <remarks>
/// The AuthController provides endpoints for:
/// - Azure AD B2C authentication integration
/// - User profile management
/// - PIN-based session validation
/// - Permission retrieval
/// Most endpoints require authentication except login and PIN validation.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPinService _pinService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, IPinService pinService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _pinService = pinService;
        _logger = logger;
    }

    /// <summary>
    /// Validates Azure AD B2C token and creates/updates user
    /// </summary>
    /// <param name="request">Login credentials including the Azure AD B2C token</param>
    /// <returns>Login response with user details and token</returns>
    /// <remarks>
    /// This endpoint expects a pre-validated Azure AD B2C token.
    /// It will create a new user record if one doesn't exist, or update existing user information.
    /// The actual token validation is performed by the authentication middleware.
    /// </remarks>
    /// <response code="200">Successfully authenticated and user record created/updated</response>
    /// <response code="401">Invalid or missing authentication token</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <returns>The authenticated user's profile information</returns>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User profile not found (should not occur for authenticated users)</response>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserAsync(userId);
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
    /// <param name="request">Updated user profile information</param>
    /// <returns>The updated user profile</returns>
    /// <response code="200">Profile successfully updated</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.UpdateUserAsync(userId, request);
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
    /// <returns>List of permission names assigned to the user</returns>
    /// <remarks>
    /// Returns a flat list of permission strings based on the user's assigned roles.
    /// Permissions follow the format: "Resource.Action" (e.g., "Device.View", "Session.Create")
    /// </remarks>
    /// <response code="200">Returns the list of permissions</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<string>>> GetPermissions()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var permissions = await _userService.GetUserPermissionsAsync(userId);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validates a session PIN for quick device access
    /// </summary>
    /// <param name="request">PIN validation request containing the PIN code and optional session ID</param>
    /// <returns>PIN validation result</returns>
    /// <remarks>
    /// This endpoint is used for PIN-based authentication, allowing quick access to remote sessions.
    /// If no session ID is provided, a new session ID will be generated.
    /// PINs are time-limited and single-use for security.
    /// </remarks>
    /// <response code="200">PIN validation result (check Success property)</response>
    [HttpPost("validate-pin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PinValidationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PinValidationResponse>> ValidatePin([FromBody] PinValidationRequest request)
    {
        try
        {
            _logger.LogInformation("PIN validation attempt");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            // Use session ID from request or generate new one
            var sessionId = request.SessionId ?? Guid.NewGuid();
            var isValid = await _pinService.ValidatePinAsync(sessionId, request.PinCode);

            var response = new PinValidationResponse
            {
                Success = isValid,
                ErrorMessage = isValid ? null : "Invalid PIN"
            };

            if (isValid)
            {
                _logger.LogInformation("PIN validated successfully for session {SessionId}", sessionId);
            }
            else
            {
                _logger.LogWarning("Invalid PIN for session {SessionId} from IP {IpAddress}", sessionId, ipAddress);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            return StatusCode(500, new PinValidationResponse
            {
                Success = false,
                ErrorMessage = "An error occurred during PIN validation"
            });
        }
    }

    /// <summary>
    /// Authenticates a host machine and returns an access token
    /// </summary>
    /// <param name="request">Host authentication credentials</param>
    /// <returns>Authentication token for the host</returns>
    /// <remarks>
    /// This endpoint is used by RemoteC.Host services to authenticate with the server.
    /// The host must provide a valid host ID and secret configured in the system.
    /// The returned token should be used in the Authorization header for subsequent requests.
    /// </remarks>
    /// <response code="200">Successfully authenticated, returns token</response>
    /// <response code="401">Invalid host credentials</response>
    [HttpPost("host/token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HostTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HostTokenResponse>> GetHostToken([FromBody] HostTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Host authentication attempt for {HostId}", request.HostId);

            // In development, accept any host with the configured dev credentials
            var validHostId = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Host:ValidId"] ?? "dev-host-001";
            var validSecret = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Host:ValidSecret"] ?? "dev-secret-001";

            if (request.HostId != validHostId || request.Secret != validSecret)
            {
                _logger.LogWarning("Invalid host credentials for {HostId}", request.HostId);
                return Unauthorized("Invalid host credentials");
            }

            // Generate a simple JWT token for the host
            var token = GenerateHostToken(request.HostId);
            
            _logger.LogInformation("Host {HostId} authenticated successfully", request.HostId);

            return Ok(new HostTokenResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = 3600 // 1 hour
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during host authentication");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Development-only endpoint for quick testing without Azure AD B2C
    /// </summary>
    /// <returns>Authentication token for development testing</returns>
    /// <remarks>
    /// This endpoint is only available when EnableDevAuth is set to true.
    /// It creates a test user token without requiring Azure AD B2C authentication.
    /// WARNING: This should NEVER be enabled in production.
    /// </remarks>
    /// <response code="200">Returns development authentication token</response>
    /// <response code="404">Endpoint not available (EnableDevAuth is false)</response>
    [HttpPost("dev-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DevLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DevLoginResponse> DevLogin()
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("EnableDevAuth", false))
        {
            return NotFound();
        }

        var devUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, devUserId.ToString()),
            new Claim("sub", devUserId.ToString()),
            new Claim(ClaimTypes.Name, "Developer User"),
            new Claim(ClaimTypes.Email, "dev@remotec.local"),
            new Claim("role", "Admin")
        };

        var jwtSecret = configuration["Jwt:Secret"] ?? "development-secret-key-for-testing-only-change-in-production";
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddHours(24);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "RemoteC",
            audience: configuration["Jwt:Audience"] ?? "RemoteC.Client",
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new DevLoginResponse
        {
            Token = tokenString,
            TokenType = "Bearer",
            ExpiresIn = 86400,
            User = new
            {
                Id = devUserId.ToString(),
                Name = "Developer User",
                Email = "dev@remotec.local",
                Roles = new[] { "Admin" }
            }
        });
    }

    /// <summary>
    /// Alternative token endpoint for compatibility
    /// </summary>
    /// <param name="request">Authentication credentials</param>
    /// <returns>Authentication token</returns>
    /// <remarks>
    /// This endpoint provides compatibility with different authentication flows.
    /// It supports both user and host authentication based on the provided credentials.
    /// </remarks>
    /// <response code="200">Successfully authenticated, returns token</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> GetToken([FromBody] TokenRequest request)
    {
        try
        {
            // Check if this is a host authentication request
            if (!string.IsNullOrEmpty(request.GrantType) && request.GrantType == "client_credentials")
            {
                var hostRequest = new HostTokenRequest
                {
                    HostId = request.ClientId ?? string.Empty,
                    Secret = request.ClientSecret ?? string.Empty
                };
                
                var hostResult = await GetHostToken(hostRequest);
                if (hostResult.Result is OkObjectResult okResult && okResult.Value is HostTokenResponse hostResponse)
                {
                    return Ok(new TokenResponse
                    {
                        AccessToken = hostResponse.Token,
                        TokenType = hostResponse.TokenType,
                        ExpiresIn = hostResponse.ExpiresIn
                    });
                }
                
                return Unauthorized("Invalid credentials");
            }

            // Default to user authentication
            return Unauthorized("Unsupported grant type");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token generation");
            return StatusCode(500, "Internal server error");
        }
    }

    private string GenerateHostToken(string hostId)
    {
        // In development, return a simple token
        // In production, this should generate a proper JWT token
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        
        // Use the same secret as DevelopmentAuthenticationHandler
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var jwtSecret = configuration["Jwt:Secret"] ?? "development-secret-key-for-testing-only-change-in-production";
        var key = System.Text.Encoding.UTF8.GetBytes(jwtSecret);
        
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", hostId),
                new Claim("type", "host"),
                new Claim(ClaimTypes.NameIdentifier, hostId)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}