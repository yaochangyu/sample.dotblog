using Lab.Cache.Test.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.Cache.Test;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddCacheProviderFactory(this IServiceCollection services,
                                                             IConfiguration configuration)
    {
        services.AddSingleton(p =>
        {
            var expiration = configuration.GetValue<TimeSpan>(nameof(Config.DEFAULT_CACHE_EXPIRATION));
            var options = new CacheProviderOptions { AbsoluteExpiration = expiration };
            return options;
        });

        services.AddMemoryCache();
        services.AddStackExchangeRedisCache((options) =>
        {
            var connectionString = configuration.GetValue<string>(nameof(Config.SYS_REDIS_URL));
            options.Configuration = connectionString;
        });

        services.AddSingleton<ICacheProviderFactory>(p =>
        {
            var memoryCache = p.GetService<IMemoryCache>();
            var distributedCache = p.GetService<IDistributedCache>();
            var cacheProviderOptions = p.GetService<CacheProviderOptions>();
            var factory = new CacheProviderFactory(memoryCache, distributedCache, cacheProviderOptions);
            return factory;
        });

        return services;
    }
}