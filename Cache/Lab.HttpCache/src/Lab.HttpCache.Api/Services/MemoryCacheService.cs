using Microsoft.Extensions.Caching.Memory;

namespace Lab.HttpCache.Api.Services;

public class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public T? Get<T>(string key)
    {
        return _memoryCache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions();

        if (absoluteExpiration.HasValue)
        {
            cacheEntryOptions.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }

        _memoryCache.Set(key, value, cacheEntryOptions);
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}
