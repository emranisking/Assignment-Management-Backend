using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Tests.Common;

public static class TestHelpers
{
    public static AppDbContext NewInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"am-tests-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}

/// <summary>A pass-through cache used in tests: always executes the factory, never caches.</summary>
public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
        => await factory();
    public Task InvalidateGroupAsync(string group, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> BuildVersionedKeyAsync(string group, string suffix, CancellationToken ct = default)
        => Task.FromResult($"{group}:{suffix}");
}
