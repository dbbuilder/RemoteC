using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace RemoteC.Api.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return await Task.FromResult(default(T));
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            var cacheOptions = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                cacheOptions.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                cacheOptions.SetSlidingExpiration(TimeSpan.FromMinutes(5)); // Default sliding expiration
            }

            _cache.Set(key, value, cacheOptions);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
            
            await Task.CompletedTask;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}, executing factory", key);
            var value = await factory();
            
            await SetAsync(key, value, expiration);
            return value;
        }

        public async Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var exists = _cache.TryGetValue(key, out _);
            return await Task.FromResult(exists);
        }

        public async Task InvalidateAsync(string pattern)
        {
            // Note: IMemoryCache doesn't support pattern-based invalidation
            // In production, you would use a distributed cache like Redis
            _logger.LogWarning("Pattern-based cache invalidation requested for pattern: {Pattern} - not supported with IMemoryCache", pattern);
            await Task.CompletedTask;
        }

        public async Task ClearAsync()
        {
            // Note: IMemoryCache doesn't have a built-in Clear method
            // In production, you might want to use a distributed cache like Redis
            _logger.LogWarning("Cache clear requested - this operation is not fully supported with IMemoryCache");
            await Task.CompletedTask;
        }
    }
}