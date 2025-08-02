using RemoteC.Data.Entities;

namespace RemoteC.Data.Repositories;

public interface IAuditRepository
{
    Task InsertAuditLogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? oldValues = null,
        string? newValues = null,
        bool success = true,
        string? errorMessage = null);

    Task<PagedResult<AuditLog>> GetAuditLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? userId = null,
        string? action = null,
        int pageNumber = 1,
        int pageSize = 50);

    Task<int> CleanupAuditLogsAsync(int retentionDays = 90);
    Task<int> GetRecentActivityCountAsync(TimeSpan timeSpan);
}