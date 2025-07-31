using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Service implementation for PIN-based authentication
    /// </summary>
    public class PinService : IPinService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<PinService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _pinLength;
        private readonly int _defaultExpirationMinutes;
        
        // In-memory storage for development
        private static readonly ConcurrentDictionary<string, PinData> _pins = new();

        public PinService(
            IDistributedCache cache,
            ILogger<PinService> logger,
            IConfiguration configuration)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _pinLength = _configuration.GetValue<int>("Security:PinLength", 6);
            _defaultExpirationMinutes = _configuration.GetValue<int>("Security:PinExpirationMinutes", 10);
        }

        public async Task<string> GeneratePinAsync(Guid sessionId)
        {
            var pin = GenerateRandomPin();
            var pinData = new PinData
            {
                SessionId = sessionId,
                Pin = pin,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_defaultExpirationMinutes),
                IsUsed = false
            };
            
            _pins[pin] = pinData;
            _logger.LogInformation("Generated PIN for session {SessionId}", sessionId);
            
            return await Task.FromResult(pin);
        }

        public async Task<bool> ValidatePinAsync(Guid sessionId, string pin)
        {
            if (!_pins.TryGetValue(pin, out var pinData))
            {
                _logger.LogWarning("Invalid PIN attempt for session {SessionId}", sessionId);
                return await Task.FromResult(false);
            }
            
            if (pinData.SessionId != sessionId)
            {
                _logger.LogWarning("PIN session mismatch for session {SessionId}", sessionId);
                return await Task.FromResult(false);
            }
            
            if (pinData.IsUsed)
            {
                _logger.LogWarning("Reused PIN attempt for session {SessionId}", sessionId);
                return await Task.FromResult(false);
            }
            
            if (DateTime.UtcNow > pinData.ExpiresAt)
            {
                _logger.LogWarning("Expired PIN attempt for session {SessionId}", sessionId);
                _pins.TryRemove(pin, out _);
                return await Task.FromResult(false);
            }
            
            pinData.IsUsed = true;
            pinData.UsedAt = DateTime.UtcNow;
            
            _logger.LogInformation("PIN validated successfully for session {SessionId}", sessionId);
            return await Task.FromResult(true);
        }

        public async Task InvalidatePinAsync(Guid sessionId)
        {
            var pinsToRemove = _pins.Where(p => p.Value.SessionId == sessionId).Select(p => p.Key).ToList();
            foreach (var pin in pinsToRemove)
            {
                _pins.TryRemove(pin, out _);
            }
            
            _logger.LogInformation("Invalidated {Count} PINs for session {SessionId}", pinsToRemove.Count, sessionId);
            await Task.CompletedTask;
        }

        public async Task<bool> IsPinValidAsync(Guid sessionId, string pin)
        {
            if (!_pins.TryGetValue(pin, out var pinData))
                return await Task.FromResult(false);
            
            return await Task.FromResult(
                pinData.SessionId == sessionId &&
                !pinData.IsUsed &&
                DateTime.UtcNow <= pinData.ExpiresAt
            );
        }

        public async Task<ExtendedPinGenerationResult> GeneratePinWithDetailsAsync(Guid sessionId, int expirationMinutes)
        {
            var pin = GenerateRandomPin();
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
            
            var pinData = new PinData
            {
                SessionId = sessionId,
                Pin = pin,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsUsed = false
            };
            
            _pins[pin] = pinData;
            
            return await Task.FromResult(new ExtendedPinGenerationResult
            {
                PinCode = pin,
                ExpiresAt = expiresAt,
                SessionId = sessionId
            });
        }

        public async Task<PinDetails?> GetPinDetailsAsync(string pin)
        {
            if (!_pins.TryGetValue(pin, out var pinData))
                return await Task.FromResult<PinDetails?>(null);
            
            return await Task.FromResult(new PinDetails
            {
                SessionId = pinData.SessionId,
                CreatedAt = pinData.CreatedAt,
                ExpiresAt = pinData.ExpiresAt,
                IsUsed = pinData.IsUsed,
                UsedAt = pinData.UsedAt
            });
        }

        public async Task<bool> RevokePinAsync(string pinCode, string userId)
        {
            if (_pins.TryRemove(pinCode, out var pinData))
            {
                _logger.LogInformation("PIN revoked by user {UserId} for session {SessionId}", userId, pinData.SessionId);
                return await Task.FromResult(true);
            }
            
            return await Task.FromResult(false);
        }

        public async Task<IEnumerable<ActivePinDto>> GetActivePinsAsync(string userId)
        {
            var activePins = _pins.Values
                .Where(p => !p.IsUsed && DateTime.UtcNow <= p.ExpiresAt)
                .Select(p => new ActivePinDto
                {
                    SessionId = p.SessionId,
                    CreatedAt = p.CreatedAt,
                    ExpiresAt = p.ExpiresAt
                })
                .ToList();
            
            return await Task.FromResult(activePins);
        }

        private string GenerateRandomPin()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[_pinLength];
            rng.GetBytes(bytes);
            
            var pin = "";
            foreach (var b in bytes)
            {
                pin += (b % 10).ToString();
            }
            
            return pin.Substring(0, _pinLength);
        }
        
        private class PinData
        {
            public Guid SessionId { get; set; }
            public string Pin { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsUsed { get; set; }
            public DateTime? UsedAt { get; set; }
        }
    }
}