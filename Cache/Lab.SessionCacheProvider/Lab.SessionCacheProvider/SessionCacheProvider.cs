using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.SessionCacheProvider;

public class SessionCacheProvider
{
    private readonly HybridCache _cache;
    private readonly ICookieAccessor _cookieAccessor;

    public SessionCacheProvider(HybridCache cache, ICookieAccessor cookieAccessor)
    {
        _cache = cache;
        _cookieAccessor = cookieAccessor;
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
        var existingId = _cookieAccessor.GetSessionId();
        if (existingId != null)
        {
            return existingId;
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _cookieAccessor.SetSessionId(sessionId);
        return sessionId;
    }
}
