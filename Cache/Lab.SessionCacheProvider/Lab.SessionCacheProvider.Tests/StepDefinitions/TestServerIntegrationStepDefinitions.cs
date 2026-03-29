using Lab.SessionCacheProvider;
using Lab.SessionCacheProvider.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lab.SessionCacheProvider.Tests.StepDefinitions;

[Binding]
public class TestServerIntegrationStepDefinitions : IDisposable
{
    private TestWebServer _fixture = null!;
    private HttpClient _client = null!;
    private HttpResponseMessage _response = null!;
    private string? _sessionCookie;

    [Given(@"一個 TestServer 應用程式")]
    public void Given一個TestServer應用程式()
    {
        _fixture = new TestWebServer();
        _client = _fixture.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    [When(@"發送 GET 請求到 ""(.*)""")]
    public void When發送GET請求到(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        _response = _client.SendAsync(request).GetAwaiter().GetResult();
        CaptureSessionCookie();
    }

    [When(@"發送 POST 請求到 ""(.*)"" 並帶入 key ""(.*)"" 值 ""(.*)""")]
    public void When發送POST請求到並帶入Key值(string path, string key, string value)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "key", key },
                { "value", value }
            })
        };
        _response = _client.SendAsync(request).GetAwaiter().GetResult();
        CaptureSessionCookie();
    }

    [When(@"帶著相同的 cookie 發送 GET 請求到 ""(.*)""")]
    public void When帶著相同的Cookie發送GET請求到(string path)
    {
        Assert.NotNull(_sessionCookie);
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"SessionCacheId={_sessionCookie}");
        _response = _client.SendAsync(request).GetAwaiter().GetResult();
    }

    [When(@"不帶 cookie 發送 GET 請求到 ""(.*)""")]
    public void When不帶Cookie發送GET請求到(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        _response = _client.SendAsync(request).GetAwaiter().GetResult();
    }

    [Then(@"Response 應包含 ""(.*)"" cookie")]
    public void ThenResponse應包含Cookie(string cookieName)
    {
        Assert.True(
            _response.Headers.TryGetValues("Set-Cookie", out var cookies),
            "Response 沒有 Set-Cookie header");

        var hasCookie = cookies!.Any(c => c.StartsWith($"{cookieName}="));
        Assert.True(hasCookie, $"Response 沒有包含 {cookieName} cookie");
    }

    [Then(@"Response 的內容應為 ""(.*)""")]
    public void ThenResponse的內容應為(string expected)
    {
        var content = _response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.Equal(expected, content);
    }

    [Then(@"Response 的內容應為空字串")]
    public void ThenResponse的內容應為空字串()
    {
        var content = _response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Assert.Equal("", content);
    }

    private void CaptureSessionCookie()
    {
        if (_response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var sessionCookie = cookies.FirstOrDefault(c => c.StartsWith("SessionCacheId="));
            if (sessionCookie != null)
            {
                _sessionCookie = sessionCookie
                    .Split(';')[0]
                    .Split('=', 2)[1];
            }
        }
    }

    public void Dispose()
    {
        _fixture?.Dispose();
        CacheSession.ResetHttpContextAccessor();
    }
}
