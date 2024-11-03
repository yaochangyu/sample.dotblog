using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Lab.Cache.Test.Caching;

public enum CacheProviderType
{
    Memory,
    Redis
}

public class CacheProviderOptions
{
    public TimeSpan? SlidingExpiration { get; set; }

    public TimeSpan? AbsoluteExpiration { get; set; }
}

public interface ICacheProviderFactory
{
    ICacheProvider Create(CacheProviderType type);
}

public class CacheProviderFactory : ICacheProviderFactory
{
    private readonly IDistributedCache _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly CacheProviderOptions _cacheProviderOptions;

    public CacheProviderFactory(IMemoryCache memoryCache,
                                IDistributedCache distributedCache,
                                CacheProviderOptions cacheProviderOptions)
    {
        this._memoryCache = memoryCache;
        this._distributedCache = distributedCache;
        this._cacheProviderOptions = cacheProviderOptions;
    }

    public ICacheProvider Create(CacheProviderType type)
    {
        return type switch
        {
            CacheProviderType.Memory => new MemoryCacheProvider(this._memoryCache, this._cacheProviderOptions),
            CacheProviderType.Redis => new RedisCacheProvider(this._distributedCache, this._cacheProviderOptions),
            _ => throw new ArgumentException($"Unsupported cache provider: {type}")
        };
    }
}