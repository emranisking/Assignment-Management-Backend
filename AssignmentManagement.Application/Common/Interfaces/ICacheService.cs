namespace AssignmentManagement.Application.Common.Interfaces;

/// <summary>
/// Distributed cache abstraction (Redis) used to cache read endpoints.
/// The global TTL is configured through the environment.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// get-or-set with the global TTL. On any cache failure it must fall back to the factory,
    /// so the API keeps working even when Redis is unavailable.
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Bumps the version stamp for an entity group so all previously cached list keys
    /// for that group become unreachable (and expire naturally).
    /// </summary>
    Task InvalidateGroupAsync(string group, CancellationToken ct = default);

    /// <summary>Builds a versioned cache key for a group (e.g. list endpoints).</summary>
    Task<string> BuildVersionedKeyAsync(string group, string suffix, CancellationToken ct = default);
}
