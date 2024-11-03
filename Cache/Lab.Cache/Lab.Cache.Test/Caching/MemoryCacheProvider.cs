using Microsoft.Extensions.Caching.Memory;

namespace Lab.Cache.Test.Caching;

public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheProviderOptions _options;

    public MemoryCacheProvider(IMemoryCache memoryCache,
                               CacheProviderOptions options = null)
    {
        this._memoryCache = memoryCache;
        this._options = options;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        this._memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key,
                            T value,
                            CacheProviderOptions options)
    {
        var cacheOptions = new MemoryCacheEntryOptions();

        if (options.SlidingExpiration.HasValue)
        {
            cacheOptions.SetSlidingExpiration(options.SlidingExpiration.Value);
        }

        if (options.AbsoluteExpiration.HasValue)
        {
            cacheOptions.SetAbsoluteExpiration(options.AbsoluteExpiration.Value);
        }

        this._memoryCache.Set(key, value, cacheOptions);
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key,
                            T value)
    {
        return this.SetAsync(key, value, this._options);
    }

    public Task RemoveAsync(string key)
    {
        this._memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(this._memoryCache.TryGetValue(key, out _));
    }
}