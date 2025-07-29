using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Client.Services
{
    public interface ISessionService
    {
        Task<List<SessionDto>> GetActiveSessionsAsync();
        Task<SessionDto?> GetSessionAsync(Guid sessionId);
        Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId);
        Task<bool> EndSessionAsync(Guid sessionId);
    }
}