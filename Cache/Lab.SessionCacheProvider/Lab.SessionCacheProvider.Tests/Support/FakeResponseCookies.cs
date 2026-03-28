using Microsoft.AspNetCore.Http;

namespace Lab.SessionCacheProvider.Tests.Support;

public class FakeResponseCookies : IResponseCookies
{
    private readonly Dictionary<string, string> _cookies = new();

    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public void Append(string key, string value)
    {
        _cookies[key] = value;
    }

    public void Append(string key, string value, CookieOptions options)
    {
        _cookies[key] = value;
    }

    public void Delete(string key)
    {
        _cookies.Remove(key);
    }

    public void Delete(string key, CookieOptions options)
    {
        _cookies.Remove(key);
    }
}
