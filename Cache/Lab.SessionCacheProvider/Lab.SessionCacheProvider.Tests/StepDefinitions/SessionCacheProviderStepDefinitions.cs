using Lab.SessionCacheProvider;
using Lab.SessionCacheProvider.Tests.Support;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.SessionCacheProvider.Tests.StepDefinitions;

[Binding]
public class SessionCacheProviderStepDefinitions
{
    private SessionCacheProvider _provider = null!;
    private SessionObject _session = null!;
    private FakeCookieAccessor _cookieAccessor = null!;

    private SessionCacheProvider CreateProvider(Dictionary<string, string>? cookies = null)
    {
        _cookieAccessor = new FakeCookieAccessor(cookies);

        var services = new ServiceCollection();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<HybridCache>();

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(20),
            LocalCacheExpiration = TimeSpan.FromMinutes(5)
        };

        return new SessionCacheProvider(cache, _cookieAccessor, entryOptions);
    }

    [Given(@"一個沒有 session cookie 的 HTTP 請求")]
    public void Given一個沒有SessionCookie的HTTP請求()
    {
        _provider = CreateProvider();
    }

    [Given(@"一個帶有 session cookie ""(.*)"" 的 HTTP 請求")]
    public void Given一個帶有SessionCookie的HTTP請求(string sessionId)
    {
        _provider = CreateProvider(new Dictionary<string, string>
        {
            { "SessionCacheId", sessionId }
        });
    }

    [When(@"存取 Session 屬性")]
    public void When存取Session屬性()
    {
        _session = _provider.Session;
    }

    [Then(@"應建立新的 session ID cookie")]
    public void Then應建立新的SessionIdCookie()
    {
        Assert.True(_cookieAccessor.WrittenCookies.ContainsKey("SessionCacheId"));
        Assert.False(string.IsNullOrEmpty(_cookieAccessor.WrittenCookies["SessionCacheId"]));
    }

    [Then(@"Session 屬性應回傳 SessionObject")]
    public void ThenSession屬性應回傳SessionObject()
    {
        Assert.NotNull(_session);
        Assert.IsType<SessionObject>(_session);
    }

    [Then(@"session ID 應為 ""(.*)""")]
    public void ThenSessionId應為(string expectedId)
    {
        Assert.NotNull(_session);
        Assert.Equal(expectedId, _cookieAccessor.WrittenCookies["SessionCacheId"]);
    }

    [When(@"透過 Session 屬性設定 key ""(.*)"" 的值為 ""(.*)""")]
    public void When透過Session屬性設定Key的值為(string key, string value)
    {
        _session = _provider.Session;
        _session[key] = value;
    }

    [Then(@"透過 Session 屬性取得 key ""(.*)"" 的值應為 ""(.*)""")]
    public void Then透過Session屬性取得Key的值應為(string key, string expected)
    {
        var session = _provider.Session;
        var result = session[key];
        Assert.NotNull(result);
        Assert.Equal(expected, result!.ToString());
    }
}
