using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace RemoteC.Api.Authentication
{
    /// <summary>
    /// Development-only authentication handler that bypasses Azure AD B2C for faster startup
    /// </summary>
    public class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public DevelopmentAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Create a fake user for development
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
    }
}