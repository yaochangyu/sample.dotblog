using Lab.SessionCacheProvider;
using Lab.SessionCacheProvider.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Lab.SessionCacheProvider.Tests.StepDefinitions;

[Binding]
public class CacheSessionStepDefinitions
{
    private object? _result;

    [Given(@"已設定 CacheSession")]
    public void Given已設定CacheSession()
    {
        var cookieAccessor = new FakeCookieAccessor();

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(20),
            LocalCacheExpiration = TimeSpan.FromMinutes(5)
        };

        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddSingleton<ICookieAccessor>(cookieAccessor);
        services.AddSingleton(entryOptions);
        services.AddScoped<SessionCacheProvider>();
        var sp = services.BuildServiceProvider();

        var httpContext = Substitute.For<HttpContext>();
        httpContext.RequestServices.Returns(sp);

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        CacheSession.SetHttpContextAccessor(accessor);
    }

    [When(@"透過 CacheSession\.Current 設定 key ""(.*)"" 的值為 ""(.*)""")]
    [Given(@"透過 CacheSession\.Current 設定 key ""(.*)"" 的值為 ""(.*)""")]
    public void When透過CacheSessionCurrent設定Key的值為(string key, string value)
    {
        CacheSession.Current[key] = value;
    }

    [When(@"透過 CacheSession\.Current 移除 key ""(.*)""")]
    public void When透過CacheSessionCurrent移除Key(string key)
    {
        CacheSession.Current.Remove(key);
    }

    [Then(@"透過 CacheSession\.Current 取得 key ""(.*)"" 的值應為 ""(.*)""")]
    public void Then透過CacheSessionCurrent取得Key的值應為(string key, string expected)
    {
        _result = CacheSession.Current[key];
        Assert.NotNull(_result);
        Assert.Equal(expected, _result!.ToString());
    }

    [Then(@"透過 CacheSession\.Current 取得 key ""(.*)"" 的值應為 null")]
    public void Then透過CacheSessionCurrent取得Key的值應為Null(string key)
    {
        _result = CacheSession.Current[key];
        Assert.Null(_result);
    }
}
