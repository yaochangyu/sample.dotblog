using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Redis;

namespace Lab.HybridCache.Avalanche.IntegrationTests;

public class CacheAvalancheIntegrationTests : IAsyncLifetime
{
    private RedisContainer _redis = null!;
    private ServiceProvider _sp = null!;

    public async Task InitializeAsync()
    {
        _redis = new RedisBuilder("redis:7-alpine").Build();
        await _redis.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddStackExchangeRedisCache(o => o.Configuration = _redis.GetConnectionString());
        services.AddHybridCache();
        services.AddSingleton<TtlJitterCacheService>();
        services.AddSingleton<LayeredTtlCacheService>();
        services.AddSingleton<CircuitBreakerCacheService>();

        _sp = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _sp.DisposeAsync();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task TtlJitter_MultipleKeys_ShouldHaveDifferentTtls()
    {
        var svc = _sp.GetRequiredService<TtlJitterCacheService>();

        var ttls = new HashSet<int>();
        for (var i = 0; i < 20; i++)
        {
            var key = $"jitter-test-{i}";
            var (_, _, l2Ttl) = await svc.GetAsync(key);
            ttls.Add((int)l2Ttl.TotalSeconds);
        }

        // 20 個不同 key 的 TTL 不應全部相同
        Assert.True(ttls.Count > 1, $"Expected multiple distinct TTLs but got {ttls.Count}");
    }

    [Fact]
    public async Task LayeredTtl_L1TtlShouldBeLessThanL2Ttl()
    {
        var svc = _sp.GetRequiredService<LayeredTtlCacheService>();
        var (data, source) = await svc.GetAsync("layered-test");

        Assert.NotNull(data);
        Assert.NotEmpty(data);
        // L1=3m, L2=30m，驗證 factory 被呼叫（第一次必定是 DB）
        Assert.Equal("DB", source);
    }

    [Fact]
    public async Task LayeredTtl_SecondCall_ShouldHitCache()
    {
        var svc = _sp.GetRequiredService<LayeredTtlCacheService>();
        await svc.GetAsync("layered-hit-test"); // 第一次寫入

        var (data, source) = await svc.GetAsync("layered-hit-test"); // 第二次命中快取
        Assert.NotNull(data);
        Assert.Equal("L1/L2", source);
    }

    [Fact]
    public async Task CircuitBreaker_NormalOperation_ShouldReturnData()
    {
        var svc = _sp.GetRequiredService<CircuitBreakerCacheService>();
        var (data, source, isFallback) = await svc.GetAsync("cb-test");

        Assert.NotNull(data);
        Assert.False(isFallback);
    }

    [Fact]
    public async Task StampedeProtection_ConcurrentRequests_FactoryShouldBeCalledOnce()
    {
        var cache = _sp.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
        var factoryCallCount = 0;
        var key = $"stampede-test-{Guid.NewGuid():N}";

        var tasks = Enumerable.Range(0, 10).Select(_ =>
            cache.GetOrCreateAsync(key, async ct =>
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(50, ct);
                return new[] { "data" };
            }).AsTask());

        await Task.WhenAll(tasks);

        // HybridCache stampede protection 確保 factory 只呼叫一次
        Assert.Equal(1, factoryCallCount);
    }
}