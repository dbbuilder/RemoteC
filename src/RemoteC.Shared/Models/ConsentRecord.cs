using System;

namespace RemoteC.Shared.Models
{
    public class ConsentRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public bool Granted { get; set; }
        public DateTime? GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public string? Details { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class PHIAccessLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PatientId { get; set; }
        public string AccessType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? DataAccessed { get; set; }
        public DateTime AccessedAt { get; set; }
        public string? IPAddress { get; set; }
        public bool Success { get; set; }
    }

    public class RetentionPolicy
    {
        public Guid Id { get; set; }
        public string DataType { get; set; } = string.Empty;
        public int RetentionDays { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}