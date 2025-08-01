using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;

namespace RemoteC.Host.Services;

public interface IAuthenticationService
{
    Task<string?> GetHostTokenAsync();
    Task<bool> ValidatePinAsync(string pin);
    Task<bool> CheckPermissionAsync(string userId, string permission);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string?> GetHostTokenAsync()
    {
        try
        {
            // Check cached token
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            // Get host credentials from configuration
            var hostId = _configuration["Host:Id"];
            var hostSecret = _configuration["Host:Secret"];
            var baseUrl = _configuration["Api:BaseUrl"];
            var tokenEndpoint = _configuration["Api:TokenEndpoint"];

            if (string.IsNullOrEmpty(hostId) || string.IsNullOrEmpty(hostSecret))
            {
                _logger.LogError("Host credentials not configured. HostId: {HostId}, Secret: {SecretConfigured}", 
                    hostId ?? "null", !string.IsNullOrEmpty(hostSecret));
                return null;
            }

            // Build full URL
            var fullUrl = tokenEndpoint.StartsWith("http") ? tokenEndpoint : $"{baseUrl}{tokenEndpoint}";

            _logger.LogInformation("Requesting host token from {TokenEndpoint} with HostId: {HostId}", 
                fullUrl, hostId);

            // Request new token
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            var requestBody = JsonSerializer.Serialize(new { hostId, secret = hostSecret });
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Token request body: {RequestBody}", requestBody);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Token response status: {StatusCode}, Content: {Content}", 
                response.StatusCode, responseContent);
            
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, options);
                
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    _cachedToken = tokenResponse.Token;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Refresh 1 minute early
                    _logger.LogInformation("Successfully obtained host token, expires in {ExpiresIn} seconds", 
                        tokenResponse.ExpiresIn);
                    return _cachedToken;
                }
                else
                {
                    _logger.LogError("Token response was successful but contained no token");
                }
            }
            else
            {
                _logger.LogError("Failed to obtain host token: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting host token");
            return null;
        }
    }

    public async Task<bool> ValidatePinAsync(string pin)
    {
        try
        {
            var token = await GetHostTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var validateEndpoint = $"{_configuration["Api:BaseUrl"]}/api/pins/validate";
            var request = new HttpRequestMessage(HttpMethod.Post, validateEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { pin }), 
                System.Text.Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PinValidationResult>(content);
                return result?.IsValid ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PIN");
            return false;
        }
    }

    public async Task<bool> CheckPermissionAsync(string userId, string permission)
    {
        try
        {
            var token = await GetHostTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var permissionEndpoint = $"{_configuration["Api:BaseUrl"]}/api/permissions/check";
            var request = new HttpRequestMessage(HttpMethod.Post, permissionEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { userId, permission }), 
                System.Text.Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PermissionCheckResult>(content);
                return result?.HasPermission ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission");
            return false;
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        
        [JsonPropertyName("tokenType")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; } = 3600; // Default to 1 hour
    }

    private class PinValidationResult
    {
        public bool IsValid { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
    }

    private class PermissionCheckResult
    {
        public bool HasPermission { get; set; }
        public string? Reason { get; set; }
    }
}