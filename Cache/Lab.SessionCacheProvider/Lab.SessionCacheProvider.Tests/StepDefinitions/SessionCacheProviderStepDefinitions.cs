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

        return new SessionCacheProvider(cache, _cookieAccessor);
    }

    [Given(@"an HTTP request without a session cookie")]
    public void GivenAnHttpRequestWithoutASessionCookie()
    {
        _provider = CreateProvider();
    }

    [Given(@"an HTTP request with session cookie ""(.*)""")]
    public void GivenAnHttpRequestWithSessionCookie(string sessionId)
    {
        _provider = CreateProvider(new Dictionary<string, string>
        {
            { "SessionCacheId", sessionId }
        });
    }

    [When(@"I access the Session property")]
    public void WhenIAccessTheSessionProperty()
    {
        _session = _provider.Session;
    }

    [Then(@"a new session ID cookie should be created")]
    public void ThenANewSessionIdCookieShouldBeCreated()
    {
        Assert.True(_cookieAccessor.WrittenCookies.ContainsKey("SessionCacheId"));
        Assert.False(string.IsNullOrEmpty(_cookieAccessor.WrittenCookies["SessionCacheId"]));
    }

    [Then(@"the Session property should return a SessionObject")]
    public void ThenTheSessionPropertyShouldReturnASessionObject()
    {
        Assert.NotNull(_session);
        Assert.IsType<SessionObject>(_session);
    }

    [Then(@"the session ID should be ""(.*)""")]
    public void ThenTheSessionIdShouldBe(string expectedId)
    {
        Assert.NotNull(_session);
        Assert.Equal(expectedId, _cookieAccessor.WrittenCookies["SessionCacheId"]);
    }

    [When(@"I set ""(.*)"" for key ""(.*)"" through the Session property")]
    public void WhenISetForKeyThroughTheSessionProperty(string value, string key)
    {
        _session = _provider.Session;
        _session[key] = value;
    }

    [Then(@"the value for key ""(.*)"" through the Session property should be ""(.*)""")]
    public void ThenTheValueForKeyThroughTheSessionPropertyShouldBe(string key, string expected)
    {
        var session = _provider.Session;
        var result = session[key];
        Assert.NotNull(result);
        Assert.Equal(expected, result!.ToString());
    }
}
