using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HttpCache.Api.Services;

public class HybridCacheService : ICacheService
{
    private readonly HybridCache _hybridCache;

    public HybridCacheService(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(5),
            LocalCacheExpiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        return await _hybridCache.GetOrCreateAsync<T>(
            key,
            async ct => await factory(ct),
            options,
            cancellationToken: cancellationToken);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(5),
            LocalCacheExpiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        await _hybridCache.SetAsync(key, value, options, cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _hybridCache.RemoveAsync(key, cancellationToken);
    }
}
