using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    public class SessionPin
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Pin { get; set; } = string.Empty;

        [Required]
        public Guid SessionId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public int AttemptsRemaining { get; set; }

        public bool IsUsed { get; set; }

        public DateTime? UsedAt { get; set; }

        // Navigation properties
        public virtual Session Session { get; set; } = null!;
    }
}