using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service for comprehensive audit logging
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log an audit entry
        /// </summary>
        Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Log multiple audit entries in batch
        /// </summary>
        Task LogBatchAsync(IEnumerable<AuditLogEntry> entries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Query audit logs with filtering
        /// </summary>
        Task<AuditLogQueryResult> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit logs for a specific resource
        /// </summary>
        Task<List<AuditLogEntry>> GetByResourceAsync(
            string resourceType, 
            string resourceId, 
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit logs for a specific user
        /// </summary>
        Task<List<AuditLogEntry>> GetByUserAsync(
            Guid userId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Export audit logs
        /// </summary>
        Task<byte[]> ExportAsync(
            AuditLogExportOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete old audit logs based on retention policy
        /// </summary>
        Task<int> DeleteOldLogsAsync(
            DateTime cutoffDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get audit statistics
        /// </summary>
        Task<AuditStatistics> GetStatisticsAsync(
            Guid organizationId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Helper method to log an action
        /// </summary>
        Task LogActionAsync(string action, string entityType, string entityId, 
            object? oldValue = null, object? newValue = null, object? metadata = null);
    }

    public class AuditLogEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
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
        public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
        public AuditCategory Category { get; set; } = AuditCategory.General;
        public string? Details { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? CorrelationId { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
    }

    // Using enums from RemoteC.Shared.Models

    public class AuditLogQuery
    {
        public Guid? OrganizationId { get; set; }
        public Guid? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Action { get; set; }
        public string? ResourceType { get; set; }
        public string? ResourceId { get; set; }
        public AuditSeverity? MinSeverity { get; set; }
        public AuditCategory? Category { get; set; }
        public string? SearchText { get; set; }
        public string? IpAddress { get; set; }
        public bool? SuccessOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "Timestamp";
        public bool SortDescending { get; set; } = true;
    }

    public class AuditLogQueryResult
    {
        public List<AuditLogEntry> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class AuditLogExportOptions
    {
        public Guid OrganizationId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ExportFormat Format { get; set; } = ExportFormat.Json;
        public bool IncludeMetadata { get; set; } = true;
        public List<AuditCategory>? Categories { get; set; }
        public List<string>? Actions { get; set; }
    }

    public enum ExportFormat
    {
        Json,
        Csv,
        Xml,
        Pdf
    }

    public class AuditStatistics
    {
        public int TotalEvents { get; set; }
        public Dictionary<string, int> EventsByAction { get; set; } = new();
        public Dictionary<AuditCategory, int> EventsByCategory { get; set; } = new();
        public Dictionary<AuditSeverity, int> EventsBySeverity { get; set; } = new();
        public Dictionary<string, int> EventsByUser { get; set; } = new();
        public Dictionary<DateTime, int> EventsByDay { get; set; } = new();
        public int FailedEvents { get; set; }
        public double AverageResponseTime { get; set; }
        public List<string> TopUsers { get; set; } = new();
        public List<string> TopActions { get; set; } = new();
        public List<AuditAlert> RecentAlerts { get; set; } = new();
    }

    public class AuditAlert
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public AuditSeverity Severity { get; set; }
        public int Count { get; set; }
    }
}