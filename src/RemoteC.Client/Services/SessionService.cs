using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Client.Services
{
    public class SessionService : ISessionService
    {
        public async Task<List<SessionDto>> GetActiveSessionsAsync()
        {
            // TODO: Get from API
            await Task.Delay(500);
            return new List<SessionDto>();
        }

        public async Task<SessionDto?> GetSessionAsync(Guid sessionId)
        {
            // TODO: Get from API
            await Task.Delay(100);
            return null;
        }

        public async Task<SessionStatistics> GetSessionStatisticsAsync(Guid sessionId)
        {
            // TODO: Get from API
            await Task.Delay(100);
            return new SessionStatistics();
        }

        public async Task<bool> EndSessionAsync(Guid sessionId)
        {
            // TODO: Call API
            await Task.Delay(500);
            return true;
        }
    }
}