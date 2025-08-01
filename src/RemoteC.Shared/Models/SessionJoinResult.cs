using System;

namespace RemoteC.Shared.Models
{
    /// <summary>
    /// Result of joining a session with PIN
    /// </summary>
    public class SessionJoinResult
    {
        /// <summary>
        /// Indicates if the join was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The session ID that was joined
        /// </summary>
        public Guid SessionId { get; set; }
        
        /// <summary>
        /// Connection token for establishing remote control connection
        /// </summary>
        public string? ConnectionToken { get; set; }
        
        /// <summary>
        /// Role assigned to the user in the session
        /// </summary>
        public string UserRole { get; set; } = "Guest";
        
        /// <summary>
        /// Error message if join failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// WebSocket URL for real-time communication
        /// </summary>
        public string? WebSocketUrl { get; set; }
        
        /// <summary>
        /// Additional connection parameters
        /// </summary>
        public Dictionary<string, object>? ConnectionParameters { get; set; }
    }
}