

namespace Application.Services.Interfaces.ExternalServices
{
    public interface ICacheService
    {
        // Retrieves a cached item by key and deserializes it to type T.
        // Returns default(T) if the key is not found or has expired.
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);


        // Serializes an object to JSON and stores it in Redis with an optional expiration time.
        Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default);


        // Removes an item from Redis by key.
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);


        // Attempts to retrieve item from cache; if missing, calls factory delegate to fetch data, 
        // populates cache, and returns the result (Cache-Aside Pattern).
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback, TimeSpan? expirationTime = null, CancellationToken cancellationToken = default);

        // Removes all cache entries that start with the specified prefix.
        Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    }
}