namespace Lab.Cache.Test.Caching;

public interface ICacheProvider
{
    Task<bool> ExistsAsync(string key);

    Task<T?> GetAsync<T>(string key);

    Task RemoveAsync(string key);

    Task SetAsync<T>(string key,
                     T value,
                     CacheProviderOptions options);

    Task SetAsync<T>(string key,
                     T value);
}