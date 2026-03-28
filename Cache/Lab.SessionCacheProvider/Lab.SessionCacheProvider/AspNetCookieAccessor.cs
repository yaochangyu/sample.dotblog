#if NETFRAMEWORK
using System.Web;

namespace Lab.SessionCacheProvider;

public class AspNetCookieAccessor : ICookieAccessor
{
    private const string CookieKey = "SessionCacheId";

    private readonly HttpContextBase _httpContext;

    public AspNetCookieAccessor(HttpContextBase httpContext)
    {
        _httpContext = httpContext;
    }

    public string? GetSessionId()
    {
        if (_httpContext.Items[CookieKey] is string cachedId)
        {
            return cachedId;
        }

        var cookie = _httpContext.Request.Cookies[CookieKey];
        if (cookie != null)
        {
            _httpContext.Items[CookieKey] = cookie.Value;
            return cookie.Value;
        }

        return null;
    }

    public void SetSessionId(string sessionId)
    {
        _httpContext.Response.Cookies.Set(new HttpCookie(CookieKey, sessionId)
        {
            HttpOnly = true,
            Path = "/"
        });
        _httpContext.Items[CookieKey] = sessionId;
    }
}
#endif
