using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ZenithAssets.Application.Common.Interfaces;

namespace ZenithAssets.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer? redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected) return default;

        try
        {
            var db = _redis.GetDatabase();
            var val = await db.StringGetAsync(key);
            if (val.IsNullOrEmpty) return default;

            return JsonSerializer.Deserialize<T>(val.ToString()!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cache for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write cache for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key {Key}", key);
        }
    }
}

public class DistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<DistributedLockService> _logger;

    public DistributedLockService(IConnectionMultiplexer? redis, ILogger<DistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> AcquireLockAsync(string resourceKey, TimeSpan expiry, TimeSpan waitTime, CancellationToken cancellationToken = default)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            // Fallback in-memory handle for local testing when redis is absent
            return new FallbackLockHandle();
        }

        var db = _redis.GetDatabase();
        var lockValue = Guid.NewGuid().ToString("N");
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < waitTime)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var acquired = await db.StringSetAsync(resourceKey, lockValue, expiry, When.NotExists);
            if (acquired)
            {
                return new RedisLockHandle(db, resourceKey, lockValue);
            }

            await Task.Delay(50, cancellationToken);
        }

        _logger.LogWarning("Could not acquire lock for {ResourceKey} within {WaitTimeMs}ms", resourceKey, waitTime.TotalMilliseconds);
        return null;
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _val;

        public RedisLockHandle(IDatabase db, string key, string val)
        {
            _db = db;
            _key = key;
            _val = val;
        }

        public async ValueTask DisposeAsync()
        {
            // Safe release via Lua script
            const string script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            await _db.ScriptEvaluateAsync(script, [new RedisKey(_key)], [new RedisValue(_val)]);
        }
    }

    private sealed class FallbackLockHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

