using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.SessionCacheProvider;

public class SessionObject
{
    private const string KeyPrefix = "session";

    private readonly HybridCache _cache;
    private readonly string _sessionId;
    private readonly HybridCacheEntryOptions _entryOptions;

    public SessionObject(HybridCache cache, string sessionId, TimeSpan? slidingExpiration = null)
    {
        _cache = cache;
        _sessionId = sessionId;
        _entryOptions = new HybridCacheEntryOptions
        {
            Expiration = slidingExpiration ?? TimeSpan.FromMinutes(20)
        };
    }

    public object? this[string key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }

    public T? Get<T>(string key)
    {
        var cacheKey = BuildCacheKey(key);
        return _cache.GetOrCreateAsync(
            cacheKey,
            _ => new ValueTask<T?>(default(T)),
            _entryOptions
        ).AsTask().GetAwaiter().GetResult();
    }

    public void Set<T>(string key, T value)
    {
        var cacheKey = BuildCacheKey(key);
        _cache.SetAsync(cacheKey, value, _entryOptions)
            .AsTask().GetAwaiter().GetResult();
    }

    public void Remove(string key)
    {
        var cacheKey = BuildCacheKey(key);
        _cache.RemoveAsync(cacheKey)
            .AsTask().GetAwaiter().GetResult();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(key);
        return await _cache.GetOrCreateAsync(
            cacheKey,
            _ => new ValueTask<T?>(default(T)),
            _entryOptions,
            cancellationToken: cancellationToken
        );
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(key);
        await _cache.SetAsync(cacheKey, value, _entryOptions, cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(key);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private object? GetValue(string key)
    {
        return Get<object>(key);
    }

    private void SetValue(string key, object? value)
    {
        if (value is null)
        {
            Remove(key);
            return;
        }

        Set(key, value);
    }

    private string BuildCacheKey(string key)
    {
        return $"{KeyPrefix}:{_sessionId}:{key}";
    }
}
