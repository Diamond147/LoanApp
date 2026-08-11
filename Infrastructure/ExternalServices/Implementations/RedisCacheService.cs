using Application.Services.Interfaces.ExternalServices;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;


namespace Infrastructure.ExternalServices.Implementations
{

    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
        }



        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var cachedString = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedString))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedString, DefaultJsonOptions);
        }


        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                // Default to 1 hour if no explicit expiration is passed
                AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromHours(1)
            };

            var jsonValue = JsonSerializer.Serialize(value, DefaultJsonOptions);
            await _cache.SetStringAsync(key, jsonValue, options, cancellationToken);
        }


        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }


        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default)
        {
            // Try to get item from cache
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // If not found in cache (Cache Miss), fetch fresh data using provided delegate
            var freshValue = await getItemCallback();
            if (freshValue != null)
            {
                // Save fetched value into cache for future requests
                await SetAsync(key, freshValue, expirationTime, cancellationToken);
            }

            return freshValue;
        }


        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // The instance prefix configured in Program.cs
            string instancePrefix = "LoanApp_";
            string searchPattern = $"{instancePrefix}{prefix}*";

            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);

                if (server.IsConnected && !server.IsReplica)
                {
                    // 1. Scan raw keys matching "LoanApp_loans:all:*"
                    var keys = server.Keys(pattern: searchPattern).ToArray();

                    foreach (var rawKey in keys)
                    {
                        string keyString = rawKey.ToString();

                        // 2. Strip "LoanApp_" so _cache.RemoveAsync doesn't turn it into "LoanApp_LoanApp_loans:all:*"
                        if (keyString.StartsWith(instancePrefix))
                        {
                            keyString = keyString.Substring(instancePrefix.Length);
                        }

                        await _cache.RemoveAsync(keyString, cancellationToken);
                    }
                }
            }
        }
    }
}