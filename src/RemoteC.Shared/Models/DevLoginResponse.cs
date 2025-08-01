namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Response model for development authentication endpoint
    /// </summary>
    public class DevLoginResponse
    {
        /// <summary>
        /// JWT authentication token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token type (always "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// User information
        /// </summary>
        public object? User { get; set; }
    }
}