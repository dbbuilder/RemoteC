using System;
using RemoteC.Api.Services;

namespace RemoteC.Data.Entities
{
    /// <summary>
    /// Audit log entity for tracking all system activities
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string? ResourceId { get; set; }
        public string? ResourceName { get; set; }
        public AuditSeverity Severity { get; set; }
        public AuditCategory Category { get; set; }
        public string? Details { get; set; }
        public string? Metadata { get; set; } // JSON serialized metadata
        public string? CorrelationId { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }

        // Navigation properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User? User { get; set; }

        // Indexes for performance
        public DateTime? IndexTimestamp { get; set; } // For time-based queries
        public string? IndexAction { get; set; } // For action-based queries
        public string? IndexResource { get; set; } // For resource-based queries
    }
}