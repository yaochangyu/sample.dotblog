#if !NETFRAMEWORK
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.SessionCacheProvider;

public static class SessionCacheProviderExtensions
{
    public static IServiceCollection AddSessionCacheProvider(
        this IServiceCollection services,
        Action<HybridCacheEntryOptions>? configureOptions = null)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(20),
            LocalCacheExpiration = TimeSpan.FromMinutes(5)
        };
        configureOptions?.Invoke(options);

        services.AddHttpContextAccessor();
        services.AddSingleton(options);
        services.AddScoped<ICookieAccessor, AspNetCoreCookieAccessor>();
        services.AddScoped<SessionCacheProvider>();
        return services;
    }
}
#endif
