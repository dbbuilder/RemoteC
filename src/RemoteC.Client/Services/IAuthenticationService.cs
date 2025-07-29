using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Client.Services
{
    public interface IAuthenticationService
    {
        Task<bool> IsAuthenticatedAsync();
        Task<AuthResult> LoginAsync();
        Task<AuthResult> LoginWithPinAsync(string deviceId, string pin);
        Task LogoutAsync();
        Task<UserDto?> GetCurrentUserAsync();
        Task<string?> GetAccessTokenAsync();
        Task<AuthResult> RefreshTokenAsync();
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ErrorMessage { get; set; }
        public UserDto? User { get; set; }
    }
}