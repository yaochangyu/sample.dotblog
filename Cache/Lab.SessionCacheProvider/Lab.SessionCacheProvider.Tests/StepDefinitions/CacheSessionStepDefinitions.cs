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

    [Given(@"a configured CacheSession")]
    public void GivenAConfiguredCacheSession()
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

    [When(@"I set ""(.*)"" for key ""(.*)"" via CacheSession\.Current")]
    [Given(@"I set ""(.*)"" for key ""(.*)"" via CacheSession\.Current")]
    public void WhenISetForKeyViaCacheSessionCurrent(string value, string key)
    {
        CacheSession.Current[key] = value;
    }

    [When(@"I remove the key ""(.*)"" via CacheSession\.Current")]
    public void WhenIRemoveTheKeyViaCacheSessionCurrent(string key)
    {
        CacheSession.Current.Remove(key);
    }

    [Then(@"the value for key ""(.*)"" via CacheSession\.Current should be ""(.*)""")]
    public void ThenTheValueForKeyViaCacheSessionCurrentShouldBe(string key, string expected)
    {
        _result = CacheSession.Current[key];
        Assert.NotNull(_result);
        Assert.Equal(expected, _result!.ToString());
    }

    [Then(@"the value for key ""(.*)"" via CacheSession\.Current should be null")]
    public void ThenTheValueForKeyViaCacheSessionCurrentShouldBeNull(string key)
    {
        _result = CacheSession.Current[key];
        Assert.Null(_result);
    }
}
