using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;
using System.Data;

namespace RemoteC.Data.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly RemoteCDbContext _context;

    public AuditRepository(RemoteCDbContext context)
    {
        _context = context;
    }

    public async Task InsertAuditLogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? oldValues = null,
        string? newValues = null,
        bool success = true,
        string? errorMessage = null)
    {
        var parameters = new[]
        {
            new SqlParameter("@Action", action),
            new SqlParameter("@EntityType", (object?)entityType ?? DBNull.Value),
            new SqlParameter("@EntityId", (object?)entityId ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
            new SqlParameter("@IpAddress", (object?)ipAddress ?? DBNull.Value),
            new SqlParameter("@UserAgent", (object?)userAgent ?? DBNull.Value),
            new SqlParameter("@OldValues", (object?)oldValues ?? DBNull.Value),
            new SqlParameter("@NewValues", (object?)newValues ?? DBNull.Value),
            new SqlParameter("@Success", success),
            new SqlParameter("@ErrorMessage", (object?)errorMessage ?? DBNull.Value)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_InsertAuditLog @Action, @EntityType, @EntityId, @UserId, @IpAddress, @UserAgent, @OldValues, @NewValues, @Success, @ErrorMessage",
            parameters);
    }

    public async Task<PagedResult<AuditLog>> GetAuditLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? userId = null,
        string? action = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var parameters = new[]
        {
            new SqlParameter("@FromDate", (object?)fromDate ?? DBNull.Value),
            new SqlParameter("@ToDate", (object?)toDate ?? DBNull.Value),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
            new SqlParameter("@Action", (object?)action ?? DBNull.Value),
            new SqlParameter("@PageNumber", pageNumber),
            new SqlParameter("@PageSize", pageSize)
        };

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_GetAuditLogs";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddRange(parameters);

        await _context.Database.OpenConnectionAsync();

        var logs = new List<AuditLog>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new AuditLog
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Action = reader.GetString(reader.GetOrdinal("Action")),
                EntityType = reader.IsDBNull(reader.GetOrdinal("EntityType")) ? null : reader.GetString(reader.GetOrdinal("EntityType")),
                EntityId = reader.IsDBNull(reader.GetOrdinal("EntityId")) ? null : reader.GetString(reader.GetOrdinal("EntityId")),
                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetGuid(reader.GetOrdinal("UserId")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                Success = reader.GetBoolean(reader.GetOrdinal("Success")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage"))
            });

            if (totalCount == 0)
                totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<AuditLog>
        {
            Items = logs,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<int> CleanupAuditLogsAsync(int retentionDays = 90)
    {
        var parameter = new SqlParameter("@RetentionDays", retentionDays);
        
        // Execute stored procedure and capture output
        var result = await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_CleanupAuditLogs @RetentionDays",
            parameter);

        return result;
    }

    public async Task<int> GetRecentActivityCountAsync(TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);
        return await _context.AuditLogs
            .Where(a => a.Timestamp >= cutoffTime)
            .CountAsync();
    }
}