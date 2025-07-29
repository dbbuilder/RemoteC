using System;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    public class PerformanceMetric
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MetricName { get; set; } = string.Empty;
        
        public double Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string? ServerId { get; set; }
        
        [MaxLength(100)]
        public string? Endpoint { get; set; }
        
        public double? Latency { get; set; }
        public double? Throughput { get; set; }
        public int? ErrorCount { get; set; }
        public int? RequestCount { get; set; }
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }

    public class UserActivityLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Details { get; set; }
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public Guid? SessionId { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Organization Organization { get; set; } = null!;
        public virtual Session? Session { get; set; }
    }

    public class BusinessEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public Guid? UserId { get; set; }
        
        [MaxLength(100)]
        public string? ProductName { get; set; }
        
        public decimal? Revenue { get; set; }
        
        [MaxLength(50)]
        public string? Currency { get; set; }
        
        [MaxLength(500)]
        public string? Properties { get; set; } // JSON serialized properties
        
        // Navigation properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User? User { get; set; }
    }

    public class CustomAlertEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MetricName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Condition { get; set; } = string.Empty; // GreaterThan, LessThan, etc.
        
        public double Threshold { get; set; }
        
        public int DurationMinutes { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty; // Info, Warning, Error, Critical
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(1000)]
        public string? NotificationChannels { get; set; } // JSON array
        
        [MaxLength(1000)]
        public string? Tags { get; set; } // JSON object
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggered { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }

    public class AlertHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid AlertId { get; set; }
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MetricName { get; set; } = string.Empty;
        
        public double CurrentValue { get; set; }
        public double ThresholdValue { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;
        
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        
        [MaxLength(1000)]
        public string? Message { get; set; }
        
        public Guid? AcknowledgedBy { get; set; }
        
        // Navigation properties
        public virtual CustomAlertEntity Alert { get; set; } = null!;
        public virtual Organization Organization { get; set; } = null!;
        public virtual User? AcknowledgedByUser { get; set; }
    }

    public class ScheduledReportEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Schedule { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Recipients { get; set; } = string.Empty; // JSON array
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        
        [MaxLength(2000)]
        public string? Parameters { get; set; } // JSON object
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public Guid CreatedBy { get; set; }
        
        // Navigation properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User Creator { get; set; } = null!;
    }

    public class DataExportRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        public Guid RequestedBy { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Status { get; set; } = "Pending";
        
        [Required]
        [MaxLength(1000)]
        public string DataTypes { get; set; } = string.Empty; // JSON array
        
        [Required]
        [MaxLength(50)]
        public string Format { get; set; } = string.Empty;
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        [MaxLength(2000)]
        public string? Filters { get; set; } // JSON object
        
        public bool CompressOutput { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        [MaxLength(500)]
        public string? DownloadUrl { get; set; }
        
        public long? FileSize { get; set; }
        public int? RecordCount { get; set; }
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
        
        // Navigation properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User RequestedByUser { get; set; } = null!;
    }

    // Additional entities for analytics aggregation
    public class HourlyAggregation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        public DateTime HourStart { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MetricType { get; set; } = string.Empty;
        
        public int Count { get; set; }
        public double Sum { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }

    public class DailyAggregation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid OrganizationId { get; set; }
        public DateTime Date { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MetricType { get; set; } = string.Empty;
        
        public int Count { get; set; }
        public double Sum { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Organization Organization { get; set; } = null!;
    }
}