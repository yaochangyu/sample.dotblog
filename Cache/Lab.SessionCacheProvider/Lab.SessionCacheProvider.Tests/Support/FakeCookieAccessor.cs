using Lab.SessionCacheProvider;

namespace Lab.SessionCacheProvider.Tests.Support;

public class FakeCookieAccessor : ICookieAccessor
{
    private readonly Dictionary<string, string> _cookies;
    private string? _cachedSessionId;

    public FakeCookieAccessor(Dictionary<string, string>? initialCookies = null)
    {
        _cookies = initialCookies ?? new Dictionary<string, string>();
    }

    public IReadOnlyDictionary<string, string> WrittenCookies => _cookies;

    public string? GetSessionId()
    {
        if (_cachedSessionId != null)
        {
            return _cachedSessionId;
        }

        if (_cookies.TryGetValue("SessionCacheId", out var id))
        {
            _cachedSessionId = id;
            return id;
        }

        return null;
    }

    public void SetSessionId(string sessionId)
    {
        _cookies["SessionCacheId"] = sessionId;
        _cachedSessionId = sessionId;
    }
}
