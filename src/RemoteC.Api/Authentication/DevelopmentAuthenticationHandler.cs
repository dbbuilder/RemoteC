using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RemoteC.Api.Authentication
{
    /// <summary>
    /// Development-only authentication handler that bypasses Azure AD B2C for faster startup
    /// </summary>
    public class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public DevelopmentAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check for Bearer token in Authorization header
            if (Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    return HandleJwtTokenAsync(token);
                }
            }

            // For non-token requests (UI), create a fake user for development
            var devUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, devUserId.ToString()),
                new Claim("sub", devUserId.ToString()), // Add sub claim as well
                new Claim(ClaimTypes.Name, "Development User"),
                new Claim(ClaimTypes.Email, "dev@localhost.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("OrganizationId", "a1b2c3d4-e5f6-7890-abcd-ef1234567890") // Default org
            };

            var identity = new ClaimsIdentity(claims, "Development");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Development");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private Task<AuthenticateResult> HandleJwtTokenAsync(string token)
        {
            try
            {
                var jwtSecret = _configuration["Jwt:Secret"] ?? "development-secret-key-for-testing-only-change-in-production";
                var key = Encoding.UTF8.GetBytes(jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

                var ticket = new AuthenticationTicket(principal, "Development");
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "JWT token validation failed");
                return Task.FromResult(AuthenticateResult.Fail($"Token validation failed: {ex.Message}"));
            }
        }
    }
}