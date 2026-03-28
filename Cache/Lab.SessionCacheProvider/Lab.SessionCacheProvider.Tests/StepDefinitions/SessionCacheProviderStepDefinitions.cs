using Lab.SessionCacheProvider;
using Lab.SessionCacheProvider.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Lab.SessionCacheProvider.Tests.StepDefinitions;

[Binding]
public class SessionCacheProviderStepDefinitions
{
    private SessionCacheProvider _provider = null!;
    private SessionObject _session = null!;
    private FakeResponseCookies _responseCookies = null!;
    private Dictionary<string, string> _requestCookies = null!;

    private SessionCacheProvider CreateProvider(Dictionary<string, string>? cookies = null)
    {
        _requestCookies = cookies ?? new Dictionary<string, string>();
        _responseCookies = new FakeResponseCookies();

        var requestCookieCollection = Substitute.For<IRequestCookieCollection>();
        requestCookieCollection.TryGetValue(Arg.Any<string>(), out Arg.Any<string?>()!)
            .Returns(callInfo =>
            {
                var key = callInfo.ArgAt<string>(0);
                if (_requestCookies.TryGetValue(key, out var value))
                {
                    callInfo[1] = value;
                    return true;
                }

                callInfo[1] = null;
                return false;
            });

        var request = Substitute.For<HttpRequest>();
        request.Cookies.Returns(requestCookieCollection);

        var response = Substitute.For<HttpResponse>();
        response.Cookies.Returns(_responseCookies);

        var items = new Dictionary<object, object?>();

        var httpContext = Substitute.For<HttpContext>();
        httpContext.Request.Returns(request);
        httpContext.Response.Returns(response);
        httpContext.Items.Returns(items);

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var services = new ServiceCollection();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        var cache = sp.GetRequiredService<HybridCache>();

        return new SessionCacheProvider(cache, accessor);
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
        Assert.True(_responseCookies.Cookies.ContainsKey("SessionCacheId"));
        Assert.False(string.IsNullOrEmpty(_responseCookies.Cookies["SessionCacheId"]));
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
        Assert.False(_responseCookies.Cookies.ContainsKey("SessionCacheId"),
            "Should not create a new cookie when one already exists.");
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
