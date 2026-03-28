#if !NETFRAMEWORK
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.SessionCacheProvider;

public static class SessionCacheProviderExtensions
{
    public static IServiceCollection AddSessionCacheProvider(
        this IServiceCollection services,
        HybridCacheOptions? cacheOptions = null)
    {
        var entryOptions = cacheOptions?.DefaultEntryOptions ?? new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(20),
            LocalCacheExpiration = TimeSpan.FromMinutes(5)
        };

        services.AddHttpContextAccessor();
        services.AddScoped<ICookieAccessor, AspNetCoreCookieAccessor>();
        services.AddScoped<SessionCacheProvider>();
        services.AddSingleton(entryOptions);
        return services;
    }

    public static IApplicationBuilder UseSessionCache(this IApplicationBuilder app)
    {
        var accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
        CacheSession.SetHttpContextAccessor(accessor);
        return app;
    }
}
#endif
