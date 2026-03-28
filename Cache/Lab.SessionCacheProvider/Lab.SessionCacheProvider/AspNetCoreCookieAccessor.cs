#if !NETFRAMEWORK
using Microsoft.AspNetCore.Http;

namespace Lab.SessionCacheProvider;

public class AspNetCoreCookieAccessor : ICookieAccessor
{
    private const string CookieKey = "SessionCacheId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AspNetCoreCookieAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetSessionId()
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

        return null;
    }

    public void SetSessionId(string sessionId)
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext 為 null，無法存取 Cookie。");

        context.Response.Cookies.Append(CookieKey, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Path = "/"
        });
        context.Items[CookieKey] = sessionId;
    }
}
#endif
