using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.Cache.Test;

public class DistributeMemoryCacheTest
{
    private static IServiceProvider CreateServiceProvider()
    {
        Environment.SetEnvironmentVariable(nameof(Config.DEFAULT_CACHE_EXPIRATION), "00:00:05");

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddEnvironmentVariables() ;
        var services = new ServiceCollection();
        var configuration = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(p =>
        {
            var expiration = configuration.GetValue<TimeSpan>(nameof(Config.DEFAULT_CACHE_EXPIRATION));

            var options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            return options;
        });
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }

    [Fact]
    public async Task 寫讀快取資料_Json()
    {
        var serviceProvider = CreateServiceProvider();
        var cache = serviceProvider.GetService<IDistributedCache>();
        var options = serviceProvider.GetService<DistributedCacheEntryOptions>();

        var key = $"{nameof(DistributeMemoryCacheTest)}:Member:1";
        var expected = JsonSerializer.Serialize(new { Name = "小心肝" });
        await cache.SetStringAsync(key, expected, options);

        var result = await cache.GetStringAsync(key);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task 寫讀快取資料_String()
    {
        var serviceProvider = CreateServiceProvider();
        var cache = serviceProvider.GetService<IDistributedCache>();
        var options = serviceProvider.GetService<DistributedCacheEntryOptions>();

        var key = $"{nameof(DistributeMemoryCacheTest)}:Member:2";
        var expected = "小心肝";
        await cache.SetStringAsync(key, expected, options);

        var result = await cache.GetStringAsync(key);
        Assert.Equal(expected, result);
    }
}