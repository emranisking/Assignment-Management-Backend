using System.Text;
using System.Text.Json;
using AssignmentManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AssignmentManagement.Infrastructure.Caching;

public class CacheOptions
{
    /// <summary>Global cache TTL. Sourced from the environment (Cache__ExpirySeconds).</summary>
    public int ExpirySeconds { get; set; } = 60;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Redis-backed cache. Every method degrades gracefully: if Redis is unreachable the API keeps
/// working by falling back to the factory. List keys are versioned per group so a single
/// InvalidateGroupAsync call retires all previously cached lists for that entity.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RedisCacheService(IDistributedCache cache, CacheOptions options, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    private TimeSpan Ttl => TimeSpan.FromSeconds(_options.ExpirySeconds <= 0 ? 60 : _options.ExpirySeconds);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!_options.Enabled) return default;
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null || bytes.Length == 0) return default;
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for {Key}; ignoring.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        if (!_options.Enabled) return;
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl ?? Ttl };
            await _cache.SetAsync(key, bytes, opts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for {Key}; ignoring.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!_options.Enabled) return;
        try { await _cache.RemoveAsync(key, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Cache REMOVE failed for {Key}.", key); }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory();
        if (value is not null) await SetAsync(key, value, ttl, ct);
        return value;
    }

    public async Task InvalidateGroupAsync(string group, CancellationToken ct = default)
    {
        if (!_options.Enabled) return;
        try
        {
            var versionKey = VersionKey(group);
            var current = await ReadVersionAsync(versionKey, ct);
            var next = (current + 1).ToString();
            await _cache.SetAsync(versionKey, Encoding.UTF8.GetBytes(next),
                new DistributedCacheEntryOptions(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache group invalidation failed for {Group}.", group);
        }
    }

    public async Task<string> BuildVersionedKeyAsync(string group, string suffix, CancellationToken ct = default)
    {
        var version = 0L;
        if (_options.Enabled)
        {
            try { version = await ReadVersionAsync(VersionKey(group), ct); }
            catch { version = 0L; }
        }
        return $"{group}:v{version}:{suffix}";
    }

    private static string VersionKey(string group) => $"cacheversion:{group}";

    private async Task<long> ReadVersionAsync(string versionKey, CancellationToken ct)
    {
        var bytes = await _cache.GetAsync(versionKey, ct);
        if (bytes is null || bytes.Length == 0) return 0L;
        return long.TryParse(Encoding.UTF8.GetString(bytes), out var v) ? v : 0L;
    }
}
