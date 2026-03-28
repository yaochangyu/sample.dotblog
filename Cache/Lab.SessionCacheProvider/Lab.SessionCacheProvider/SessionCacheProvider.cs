using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.SessionCacheProvider;

public class SessionCacheProvider
{
    private readonly HybridCache _cache;
    private readonly ICookieAccessor _cookieAccessor;
    private readonly HybridCacheEntryOptions _entryOptions;

    public SessionCacheProvider(
        HybridCache cache,
        ICookieAccessor cookieAccessor,
        HybridCacheEntryOptions entryOptions)
    {
        _cache = cache;
        _cookieAccessor = cookieAccessor;
        _entryOptions = entryOptions;
    }

    public SessionObject Session
    {
        get
        {
            var sessionId = GetOrCreateSessionId();
            return new SessionObject(_cache, sessionId, _entryOptions);
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
