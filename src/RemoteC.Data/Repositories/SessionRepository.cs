using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using System.Data;

namespace RemoteC.Data.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly RemoteCDbContext _context;

    public SessionRepository(RemoteCDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SessionSummary>> GetUserSessionsAsync(Guid userId, int pageNumber = 1, int pageSize = 25)
    {
        var parameters = new[]
        {
            new SqlParameter("@UserId", userId),
            new SqlParameter("@PageNumber", pageNumber),
            new SqlParameter("@PageSize", pageSize)
        };

        // Execute stored procedure and get results
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_GetUserSessions";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddRange(parameters);

        await _context.Database.OpenConnectionAsync();

        var sessions = new List<SessionSummary>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sessions.Add(new SessionSummary
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Status = (SessionStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Type = (SessionType)reader.GetInt32(reader.GetOrdinal("Type")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                EndedAt = reader.IsDBNull(reader.GetOrdinal("EndedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("EndedAt")),
                DeviceName = reader.GetString(reader.GetOrdinal("DeviceName")),
                HostName = reader.GetString(reader.GetOrdinal("HostName")),
                CreatedByName = reader.GetString(reader.GetOrdinal("CreatedByName"))
            });

            if (totalCount == 0)
                totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<SessionSummary>
        {
            Items = sessions,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<SessionDetails?> GetSessionDetailsAsync(Guid sessionId, Guid? userId = null)
    {
        var parameters = new[]
        {
            new SqlParameter("@SessionId", sessionId),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value)
        };

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_GetSessionDetails";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddRange(parameters);

        await _context.Database.OpenConnectionAsync();

        SessionDetails? sessionDetails = null;
        var participants = new List<SessionParticipantInfo>();

        using var reader = await command.ExecuteReaderAsync();
        
        // Read session details
        if (await reader.ReadAsync())
        {
            sessionDetails = new SessionDetails
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Status = (SessionStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Type = (SessionType)reader.GetInt32(reader.GetOrdinal("Type")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                EndedAt = reader.IsDBNull(reader.GetOrdinal("EndedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("EndedAt")),
                ConnectionInfo = reader.IsDBNull(reader.GetOrdinal("ConnectionInfo")) ? null : reader.GetString(reader.GetOrdinal("ConnectionInfo")),
                RequirePin = reader.GetBoolean(reader.GetOrdinal("RequirePin")),
                DeviceId = reader.GetGuid(reader.GetOrdinal("DeviceId")),
                DeviceName = reader.GetString(reader.GetOrdinal("DeviceName")),
                HostName = reader.GetString(reader.GetOrdinal("HostName")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
                OperatingSystem = reader.IsDBNull(reader.GetOrdinal("OperatingSystem")) ? null : reader.GetString(reader.GetOrdinal("OperatingSystem")),
                CreatedById = reader.GetGuid(reader.GetOrdinal("CreatedById")),
                CreatedByName = reader.GetString(reader.GetOrdinal("CreatedByName"))
            };
        }

        // Read participants
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                participants.Add(new SessionParticipantInfo
                {
                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                    Role = (ParticipantRole)reader.GetInt32(reader.GetOrdinal("Role")),
                    IsConnected = reader.GetBoolean(reader.GetOrdinal("IsConnected")),
                    JoinedAt = reader.GetDateTime(reader.GetOrdinal("JoinedAt")),
                    LeftAt = reader.IsDBNull(reader.GetOrdinal("LeftAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LeftAt")),
                    UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
        }

        if (sessionDetails != null)
        {
            sessionDetails.Participants = participants;
        }

        return sessionDetails;
    }

    public async Task<Session> CreateSessionAsync(string name, Guid deviceId, Guid createdBy, SessionType type = SessionType.RemoteControl, bool requirePin = true)
    {
        var sessionIdParam = new SqlParameter("@SessionId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };

        var parameters = new[]
        {
            sessionIdParam,
            new SqlParameter("@Name", name),
            new SqlParameter("@DeviceId", deviceId),
            new SqlParameter("@CreatedBy", createdBy),
            new SqlParameter("@Type", (int)type),
            new SqlParameter("@RequirePin", requirePin)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_CreateSession @SessionId OUTPUT, @Name, @DeviceId, @CreatedBy, @Type, @RequirePin",
            parameters);

        var sessionId = (Guid)sessionIdParam.Value;
        return (await _context.Sessions.FindAsync(sessionId))!;
    }

    public async Task UpdateSessionStatusAsync(Guid sessionId, SessionStatus status, Guid? userId = null, string? connectionInfo = null)
    {
        var parameters = new[]
        {
            new SqlParameter("@SessionId", sessionId),
            new SqlParameter("@Status", (int)status),
            new SqlParameter("@UserId", (object?)userId ?? DBNull.Value),
            new SqlParameter("@ConnectionInfo", (object?)connectionInfo ?? DBNull.Value)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_UpdateSessionStatus @SessionId, @Status, @UserId, @ConnectionInfo",
            parameters);
    }

    public async Task<bool> AddParticipantAsync(Guid sessionId, Guid userId, ParticipantRole role)
    {
        var participant = new SessionParticipant
        {
            SessionId = sessionId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsConnected = false
        };

        _context.SessionParticipants.Add(participant);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RemoveParticipantAsync(Guid sessionId, Guid userId)
    {
        var participant = await _context.SessionParticipants
            .Where(sp => sp.SessionId == sessionId && sp.UserId == userId)
            .FirstOrDefaultAsync();

        if (participant != null)
        {
            participant.LeftAt = DateTime.UtcNow;
            participant.IsConnected = false;
            return await _context.SaveChangesAsync() > 0;
        }

        return false;
    }

    public async Task<bool> UpdateParticipantStatusAsync(Guid sessionId, Guid userId, bool isConnected)
    {
        var participant = await _context.SessionParticipants
            .Where(sp => sp.SessionId == sessionId && sp.UserId == userId)
            .FirstOrDefaultAsync();

        if (participant != null)
        {
            participant.IsConnected = isConnected;
            if (!isConnected && !participant.LeftAt.HasValue)
            {
                participant.LeftAt = DateTime.UtcNow;
            }
            return await _context.SaveChangesAsync() > 0;
        }

        return false;
    }

    public async Task<IEnumerable<SessionParticipant>> GetSessionParticipantsAsync(Guid sessionId)
    {
        return await _context.SessionParticipants
            .Where(sp => sp.SessionId == sessionId)
            .Include(sp => sp.User)
            .AsNoTracking()
            .ToListAsync();
    }
}