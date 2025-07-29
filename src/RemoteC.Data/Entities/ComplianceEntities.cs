using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    public class ComplianceSettings
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        // SOC2 Settings
        public bool EnableMFA { get; set; }
        public bool EnableEncryption { get; set; }
        public bool EnableTLS { get; set; }
        public bool EnableAuditLogging { get; set; }
        public bool HasAccessReviews { get; set; }
        public bool HasMonitoring { get; set; }
        public bool HasBackupProcedures { get; set; }
        public bool HasDataValidation { get; set; }
        public bool HasDataClassification { get; set; }
        
        // HIPAA Settings
        public bool HasSecurityOfficer { get; set; }
        public bool HasPhysicalAccessControls { get; set; }
        public bool HasWorkstationSecurity { get; set; }
        public bool HasMediaControls { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }

    public class PrivacyPolicy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;
        
        public DateTime EffectiveDate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        [MaxLength(255)]
        public string UpdatedBy { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true; // Added for test compatibility
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }

    public class DataProcessingAgreement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string ProcessorName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime SignedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        [MaxLength(255)]
        public string SignedBy { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }

    public class ConsentRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Purpose { get; set; } = string.Empty;
        
        public bool Granted { get; set; }
        public DateTime? GrantedAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        
        [MaxLength(500)]
        public string? Details { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(255)]
        public string UpdatedBy { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Organization Organization { get; set; } = null!;
    }

    public class RetentionPolicy
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(100)]
        public string DataType { get; set; } = string.Empty;
        
        public int RetentionDays { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class PHIAccessLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid UserId { get; set; }
        public Guid PatientId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string AccessType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? DataAccessed { get; set; }
        
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(45)]
        public string? IPAddress { get; set; }
        
        public bool Success { get; set; }
        
        // Navigation property
        public virtual User User { get; set; } = null!;
    }

    public class DataBreach
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        public DateTime DiscoveryDate { get; set; }
        public DateTime IncidentDate { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string DataTypesInvolved { get; set; } = string.Empty;
        
        public int AffectedIndividuals { get; set; }
        
        [MaxLength(255)]
        public string ReportedBy { get; set; } = string.Empty;
        
        public bool NotificationSent { get; set; }
        public DateTime? NotificationSentDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }
}