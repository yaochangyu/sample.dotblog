using Lab.SessionCacheProvider;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.SessionCacheProvider.Tests.StepDefinitions;

[Binding]
public class SessionObjectStepDefinitions
{
    private SessionObject _sessionObject = null!;
    private object? _result;
    private int _intResult;

    [Given(@"a SessionObject instance")]
    public void GivenASessionObjectInstance()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(20),
            LocalCacheExpiration = TimeSpan.FromMinutes(20)
        };
        _sessionObject = new SessionObject(cache, Guid.NewGuid().ToString("N"), entryOptions);
    }

    [When(@"I set the value ""(.*)"" for key ""(.*)""")]
    [Given(@"I set the value ""(.*)"" for key ""(.*)""")]
    public void WhenISetTheValueForKey(string value, string key)
    {
        _sessionObject[key] = value;
    }

    [When(@"I set null for key ""(.*)""")]
    public void WhenISetNullForKey(string key)
    {
        _sessionObject[key] = null;
    }

    [When(@"I remove the key ""(.*)""")]
    public void WhenIRemoveTheKey(string key)
    {
        _sessionObject.Remove(key);
    }

    [When(@"I set the integer value (\d+) for key ""(.*)""")]
    public void WhenISetTheIntegerValueForKey(int value, string key)
    {
        _sessionObject.Set(key, value);
    }

    [Then(@"the value for key ""(.*)"" should be ""(.*)""")]
    public void ThenTheValueForKeyShouldBe(string key, string expected)
    {
        _result = _sessionObject[key];
        Assert.NotNull(_result);
        Assert.Equal(expected, _result!.ToString());
    }

    [Then(@"the value for key ""(.*)"" should be null")]
    public void ThenTheValueForKeyShouldBeNull(string key)
    {
        _result = _sessionObject[key];
        Assert.Null(_result);
    }

    [Then(@"the integer value for key ""(.*)"" should be (\d+)")]
    public void ThenTheIntegerValueForKeyShouldBe(string key, int expected)
    {
        _intResult = _sessionObject.Get<int>(key);
        Assert.Equal(expected, _intResult);
    }
}
