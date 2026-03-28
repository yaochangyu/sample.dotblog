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

    [Given(@"一個 SessionObject 實例")]
    public void Given一個SessionObject實例()
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

    [When(@"設定 key ""(.*)"" 的值為 ""(.*)""")]
    [Given(@"設定 key ""(.*)"" 的值為 ""(.*)""")]
    public void When設定Key的值為(string key, string value)
    {
        _sessionObject[key] = value;
    }

    [When(@"設定 key ""(.*)"" 的值為 null")]
    public void When設定Key的值為Null(string key)
    {
        _sessionObject[key] = null;
    }

    [When(@"移除 key ""(.*)""")]
    public void When移除Key(string key)
    {
        _sessionObject.Remove(key);
    }

    [When(@"設定 key ""(.*)"" 的整數值為 (\d+)")]
    public void When設定Key的整數值為(string key, int value)
    {
        _sessionObject.Set(key, value);
    }

    [Then(@"key ""(.*)"" 的值應為 ""(.*)""")]
    public void ThenKey的值應為(string key, string expected)
    {
        _result = _sessionObject[key];
        Assert.NotNull(_result);
        Assert.Equal(expected, _result!.ToString());
    }

    [Then(@"key ""(.*)"" 的值應為 null")]
    public void ThenKey的值應為Null(string key)
    {
        _result = _sessionObject[key];
        Assert.Null(_result);
    }

    [Then(@"key ""(.*)"" 的整數值應為 (\d+)")]
    public void ThenKey的整數值應為(string key, int expected)
    {
        _intResult = _sessionObject.Get<int>(key);
        Assert.Equal(expected, _intResult);
    }
}
