using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Data.Repositories;

public interface ISessionRepository
{
    Task<PagedResult<SessionSummary>> GetUserSessionsAsync(Guid userId, int pageNumber = 1, int pageSize = 25);
    Task<SessionDetails?> GetSessionDetailsAsync(Guid sessionId, Guid? userId = null);
    Task<Session> CreateSessionAsync(string name, Guid deviceId, Guid createdBy, RemoteC.Data.Entities.SessionType type = RemoteC.Data.Entities.SessionType.RemoteControl, bool requirePin = true);
    Task UpdateSessionStatusAsync(Guid sessionId, RemoteC.Data.Entities.SessionStatus status, Guid? userId = null, string? connectionInfo = null);
    Task<bool> AddParticipantAsync(Guid sessionId, Guid userId, RemoteC.Data.Entities.ParticipantRole role);
    Task<bool> RemoveParticipantAsync(Guid sessionId, Guid userId);
    Task<bool> UpdateParticipantStatusAsync(Guid sessionId, Guid userId, bool isConnected);
    Task<IEnumerable<SessionParticipant>> GetSessionParticipantsAsync(Guid sessionId);
}