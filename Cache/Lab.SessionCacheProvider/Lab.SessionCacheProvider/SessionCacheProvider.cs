using Microsoft.Extensions.Caching.Hybrid;

#if NETFRAMEWORK
using System.Web;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Lab.SessionCacheProvider;

public class SessionCacheProvider
{
    private const string CookieKey = "SessionCacheId";

#if NETFRAMEWORK
    private static HybridCache? s_cache;

    public static void Initialize(HybridCache cache)
    {
        s_cache = cache;
    }

    public static SessionObject Session
    {
        get
        {
            if (s_cache is null)
            {
                throw new InvalidOperationException(
                    "SessionCacheProvider 尚未初始化，請先呼叫 SessionCacheProvider.Initialize(HybridCache)。");
            }

            var sessionId = GetOrCreateSessionId();
            return new SessionObject(s_cache, sessionId);
        }
    }

    private static string GetOrCreateSessionId()
    {
        var context = HttpContext.Current
            ?? throw new InvalidOperationException("HttpContext.Current 為 null，無法存取 Cookie。");

        if (context.Items[CookieKey] is string cachedId)
        {
            return cachedId;
        }

        var cookie = context.Request.Cookies[CookieKey];
        if (cookie != null)
        {
            context.Items[CookieKey] = cookie.Value;
            return cookie.Value;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Set(new HttpCookie(CookieKey, sessionId)
        {
            HttpOnly = true,
            Path = "/"
        });
        context.Items[CookieKey] = sessionId;

        return sessionId;
    }
#else
    private readonly HybridCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionCacheProvider(HybridCache cache, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
    }

    public SessionObject Session
    {
        get
        {
            var sessionId = GetOrCreateSessionId();
            return new SessionObject(_cache, sessionId);
        }
    }

    private string GetOrCreateSessionId()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext 為 null，無法存取 Cookie。");

        if (context.Items.TryGetValue(CookieKey, out var cachedId) && cachedId is string id)
        {
            return id;
        }

        if (context.Request.Cookies.TryGetValue(CookieKey, out var existingId))
        {
            context.Items[CookieKey] = existingId;
            return existingId;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(CookieKey, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Path = "/"
        });
        context.Items[CookieKey] = sessionId;

        return sessionId;
    }
#endif
}
