using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    /// <summary>
    /// PHI (Protected Health Information) access tracking entity
    /// </summary>
    public class PHIAccess
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid UserId { get; set; }
        public Guid PatientId { get; set; }
        public Guid ResourceId { get; set; } // Added for test compatibility
        
        [Required]
        [MaxLength(100)]
        public string AccessType { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;
        
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow; // Added for test compatibility
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30); // Added for test compatibility
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}