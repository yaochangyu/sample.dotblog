#if NETFRAMEWORK
using System.Web;

namespace Lab.SessionCacheProvider;

public class AspNetCookieAccessor : ICookieAccessor
{
    private readonly HttpContextBase _httpContext;

    public AspNetCookieAccessor(HttpContextBase httpContext)
    {
        _httpContext = httpContext;
    }

    public string? GetSessionId()
    {
        if (_httpContext.Items[SessionCacheConstants.CookieKey] is string cachedId)
        {
            return cachedId;
        }

        var cookie = _httpContext.Request.Cookies[SessionCacheConstants.CookieKey];
        if (cookie != null)
        {
            _httpContext.Items[SessionCacheConstants.CookieKey] = cookie.Value;
            return cookie.Value;
        }

        return null;
    }

    public void SetSessionId(string sessionId)
    {
        _httpContext.Response.Cookies.Set(new HttpCookie(SessionCacheConstants.CookieKey, sessionId)
        {
            HttpOnly = true,
            Path = "/"
        });
        _httpContext.Items[SessionCacheConstants.CookieKey] = sessionId;
    }
}
#endif
