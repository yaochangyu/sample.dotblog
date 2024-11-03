using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.Cache.Test;

public class MemoryCacheTest
{
    private static IServiceProvider CreateServiceProvider()
    {
        Environment.SetEnvironmentVariable(nameof(Config.DEFAULT_CACHE_EXPIRATION), "00:00:05");

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddEnvironmentVariables() ;
        var services = new ServiceCollection();
        var configuration = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(p =>
        {
            var expiration = configuration.GetValue<TimeSpan>(nameof(Config.DEFAULT_CACHE_EXPIRATION));

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            return options;
        });
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }

    [Fact]
    public async Task 寫讀快取資料_Json_Memory()
    {
        var serviceProvider = CreateServiceProvider();
        var cache = serviceProvider.GetService<IMemoryCache>();
        var options = serviceProvider.GetService<MemoryCacheEntryOptions>();

        var key = $"{nameof(MemoryCacheTest)}:Member:1";
        var expected = JsonSerializer.Serialize(new { Name = "小心肝" });
        cache.Set(key, expected, options);

        var result = cache.Get<string>(key);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task 寫讀快取資料_String_Memory()
    {
        var serviceProvider = CreateServiceProvider();
        var cache = serviceProvider.GetService<IMemoryCache>();
        var options = serviceProvider.GetService<MemoryCacheEntryOptions>();

        var key = $"{nameof(MemoryCacheTest)}:Member:2";
        var expected = "小心肝";
        cache.Set(key, expected, options);

        var result = cache.Get<string>(key);
        Assert.Equal(expected, result);
    }
}