using Microsoft.Extensions.Caching.Hybrid;

#if NETFRAMEWORK
using System.Web;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Lab.SessionCacheProvider;

public static class CacheSession
{
#if NETFRAMEWORK
    private static HybridCache? s_cache;
    private static HybridCacheEntryOptions? s_entryOptions;

    public static void Initialize(HybridCache cache, HybridCacheEntryOptions entryOptions)
    {
        s_cache = cache;
        s_entryOptions = entryOptions;
    }

    public static SessionObject Current
    {
        get
        {
            if (s_cache is null || s_entryOptions is null)
            {
                throw new InvalidOperationException(
                    "CacheSession 尚未初始化，請先呼叫 CacheSession.Initialize()。");
            }

            var httpContext = HttpContext.Current
                ?? throw new InvalidOperationException("HttpContext.Current 為 null。");

            var cookieAccessor = new AspNetCookieAccessor(new HttpContextWrapper(httpContext));
            var provider = new SessionCacheProvider(s_cache, cookieAccessor, s_entryOptions);
            return provider.Session;
        }
    }
#else
    public static SessionObject Current
    {
        get
        {
            var httpContext = ResolveHttpContext();
            var provider = httpContext.RequestServices.GetRequiredService<SessionCacheProvider>();
            return provider.Session;
        }
    }

    private static HttpContext ResolveHttpContext()
    {
        var accessor = _httpContextAccessor
            ?? throw new InvalidOperationException(
                "CacheSession 尚未初始化，請先呼叫 app.UseSessionCache()。");

        return accessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext 為 null。");
    }

    private static IHttpContextAccessor? _httpContextAccessor;

    internal static void SetHttpContextAccessor(IHttpContextAccessor accessor)
    {
        _httpContextAccessor = accessor;
    }
#endif
}
