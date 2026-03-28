namespace Lab.SessionCacheProvider;

public interface ICookieAccessor
{
    string? GetSessionId();

    void SetSessionId(string sessionId);
}
