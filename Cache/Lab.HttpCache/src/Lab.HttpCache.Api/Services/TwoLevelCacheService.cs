namespace Lab.HttpCache.Api.Services;

public class TwoLevelCacheService : ITwoLevelCacheService
{
    private readonly IMemoryCacheService _memoryCacheService;
    private readonly IRedisCacheService _redisCacheService;

    public TwoLevelCacheService(
        IMemoryCacheService memoryCacheService,
        IRedisCacheService redisCacheService)
    {
        _memoryCacheService = memoryCacheService;
        _redisCacheService = redisCacheService;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        // 先從一級快取 (Memory) 取得
        var value = _memoryCacheService.Get<T>(key);
        if (value != null)
        {
            return value;
        }

        // 一級快取未命中，從二級快取 (Redis) 取得
        value = await _redisCacheService.GetAsync<T>(key);
        if (value != null)
        {
            // 將資料回寫到一級快取
            _memoryCacheService.Set(key, value);
            return value;
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        // 同時寫入一級和二級快取
        _memoryCacheService.Set(key, value, absoluteExpiration);
        await _redisCacheService.SetAsync(key, value, absoluteExpiration);
    }

    public async Task RemoveAsync(string key)
    {
        // 同時移除一級和二級快取
        _memoryCacheService.Remove(key);
        await _redisCacheService.RemoveAsync(key);
    }
}
