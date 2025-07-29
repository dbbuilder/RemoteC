using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using RemoteC.Shared.Models;
using Serilog;

namespace RemoteC.Client.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger _logger = Log.ForContext<AuthenticationService>();
        private readonly IConfiguration _configuration;
        private IPublicClientApplication? _publicClientApp;
        private AuthResult? _currentAuth;

        public AuthenticationService(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeMsal();
        }

        private void InitializeMsal()
        {
            var authConfig = _configuration.GetSection("RemoteC:Authentication");
            var clientId = authConfig["ClientId"];
            var authority = authConfig["Authority"];
            var redirectUri = authConfig["RedirectUri"];

            _publicClientApp = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(authority)
                .WithRedirectUri(redirectUri)
                .WithLogging((level, message, containsPii) =>
                {
                    _logger.Debug("MSAL: {Message}", message);
                })
                .Build();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var accounts = await _publicClientApp!.GetAccountsAsync();
                return accounts.Any();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking authentication status");
                return false;
            }
        }

        public async Task<AuthResult> LoginAsync()
        {
            try
            {
                var scopes = _configuration.GetSection("RemoteC:Authentication:Scopes").Get<string[]>();
                var result = await _publicClientApp!.AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();

                _currentAuth = new AuthResult
                {
                    Success = true,
                    AccessToken = result.AccessToken,
                    User = new UserDto
                    {
                        Id = Guid.Parse(result.UniqueId),
                        Email = result.Account.Username,
                        FirstName = "User",
                        LastName = ""
                    }
                };

                return _currentAuth;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Login failed");
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<AuthResult> LoginWithPinAsync(string deviceId, string pin)
        {
            // TODO: Implement PIN-based authentication
            await Task.Delay(500); // Simulate network call
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "PIN authentication not yet implemented"
            };
        }

        public async Task LogoutAsync()
        {
            try
            {
                var accounts = await _publicClientApp!.GetAccountsAsync();
                foreach (var account in accounts)
                {
                    await _publicClientApp.RemoveAsync(account);
                }
                _currentAuth = null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Logout failed");
            }
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            if (_currentAuth?.User != null)
                return _currentAuth.User;

            var accounts = await _publicClientApp!.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            if (account != null)
            {
                return new UserDto
                {
                    Id = Guid.NewGuid(), // Would come from token claims
                    Email = account.Username,
                    FirstName = "User",
                    LastName = ""
                };
            }

            return null;
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                var accounts = await _publicClientApp!.GetAccountsAsync();
                var account = accounts.FirstOrDefault();
                if (account == null)
                    return null;

                var scopes = _configuration.GetSection("RemoteC:Authentication:Scopes").Get<string[]>();
                var result = await _publicClientApp.AcquireTokenSilent(scopes, account).ExecuteAsync();
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get access token");
                return null;
            }
        }

        public async Task<AuthResult> RefreshTokenAsync()
        {
            var token = await GetAccessTokenAsync();
            if (token != null)
            {
                return new AuthResult
                {
                    Success = true,
                    AccessToken = token
                };
            }

            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Failed to refresh token"
            };
        }
    }
}