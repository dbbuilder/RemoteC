namespace RemoteC.Shared.Models;

/// <summary>
/// Request model for host authentication
/// </summary>
public class HostTokenRequest
{
    /// <summary>
    /// The unique identifier for the host machine
    /// </summary>
    public string HostId { get; set; } = string.Empty;
    
    /// <summary>
    /// The secret key for authentication
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}

/// <summary>
/// Response model for host authentication
/// </summary>
public class HostTokenResponse
{
    /// <summary>
    /// The JWT authentication token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// The token type (typically "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Generic token request for OAuth2-style authentication
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// The grant type (e.g., "client_credentials", "password")
    /// </summary>
    public string? GrantType { get; set; }
    
    /// <summary>
    /// Client ID for client credentials flow
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client secret for client credentials flow
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Username for password flow
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// Password for password flow
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// Requested scopes
    /// </summary>
    public string? Scope { get; set; }
}

/// <summary>
/// Generic token response
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// The access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The token type (typically "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// Refresh token (optional)
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Granted scopes (optional)
    /// </summary>
    public string? Scope { get; set; }
}