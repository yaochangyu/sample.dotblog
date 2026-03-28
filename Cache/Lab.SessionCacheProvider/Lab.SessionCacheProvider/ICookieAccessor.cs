namespace Lab.SessionCacheProvider;

public static class SessionCacheConstants
{
    public const string CookieKey = "SessionCacheId";
}

public interface ICookieAccessor
{
    string? GetSessionId();

    void SetSessionId(string sessionId);
}
