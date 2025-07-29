using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    /// <summary>
    /// User activity entity for tracking user actions
    /// </summary>
    public class UserActivity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}