#if !NETFRAMEWORK
using Microsoft.Extensions.DependencyInjection;

namespace Lab.SessionCacheProvider;

public static class SessionCacheProviderExtensions
{
    public static IServiceCollection AddSessionCacheProvider(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICookieAccessor, AspNetCoreCookieAccessor>();
        services.AddScoped<SessionCacheProvider>();
        return services;
    }
}
#endif
