using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    public class Organization
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Domain { get; set; }
        
        [MaxLength(255)]
        public string? DataProtectionOfficerEmail { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ComplianceSettings? ComplianceSettings { get; set; }
        public virtual PrivacyPolicy? PrivacyPolicy { get; set; }
        public virtual ICollection<DataProcessingAgreement> DataProcessingAgreements { get; set; } = new List<DataProcessingAgreement>();
    }
}