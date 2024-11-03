using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Lab.Cache.Test.Caching;

public class RedisCacheProvider : ICacheProvider
{
    private readonly IDistributedCache _distributedCache;
    private readonly CacheProviderOptions _options;

    public RedisCacheProvider(IDistributedCache distributedCache,
                              CacheProviderOptions options = null)
    {
        this._distributedCache = distributedCache;
        this._options = options;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var jsonData = await this._distributedCache.GetStringAsync(key);
        if (string.IsNullOrEmpty(jsonData))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(jsonData);
    }

    public async Task SetAsync<T>(string key,
                                  T value,
                                  CacheProviderOptions options)
    {
        var distributedOptions = new DistributedCacheEntryOptions();

        if (options.SlidingExpiration.HasValue)
        {
            distributedOptions.SetSlidingExpiration(options.SlidingExpiration.Value);
        }

        if (options.AbsoluteExpiration.HasValue)
        {
            distributedOptions.SetAbsoluteExpiration(options.AbsoluteExpiration.Value);
        }

        var jsonData = JsonSerializer.Serialize(value);
        await this._distributedCache.SetStringAsync(key, jsonData, distributedOptions);
    }

    public Task SetAsync<T>(string key,
                            T value)
    {
        return this.SetAsync(key, value, this._options);
    }

    public async Task RemoveAsync(string key)
    {
        await this._distributedCache.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var value = await this._distributedCache.GetStringAsync(key);
        return !string.IsNullOrEmpty(value);
    }
}