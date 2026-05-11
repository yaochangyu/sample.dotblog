---
title: '[.NET 10] HybridCache 快取雪崩防護三策略實作'
abstract: '系統重啟之後，所有快取全空，大量請求同時打到 DB，DB 直接噴掉了啦!!! 這就是快取雪崩（Cache Avalanche）的經典場景。 本文用 .NET 10 的 HybridCache 示範三種防護策略：TTL Jitter、分層 TTL、Circuit Breaker，並搭配整合測試驗證行為。 '
keywords: ''
categories: ''
weblogName: My Blog
postId: 1c71829f-77ca-418d-99b0-e09d14a079a6
postDate: 2026-05-10T22:11:54.9289390+08:00
postStatus: publish
dontInferFeaturedImage: false
stripH1Header: true
---
# [.NET 10] HybridCache 快取雪崩防護三策略實作

系統重啟之後，所有快取全空，大量請求同時打到 DB，DB 直接噴掉了啦!!!
這就是快取雪崩（Cache Avalanche）的經典場景。
本文用 .NET 10 的 HybridCache 示範三種防護策略：TTL Jitter、分層 TTL、Circuit Breaker，並搭配整合測試驗證行為。


---

## 開發環境

- OS：Windows 11 + WSL2
- .NET：10.0
- Microsoft.Extensions.Caching.Hybrid：10.0.*
- Microsoft.Extensions.Caching.StackExchangeRedis：10.0.*
- Polly.Extensions：8.6.6
- Testcontainers.Redis：4.11.0
- xUnit：2.9.3
- Redis：7 (Docker)

---

## 快取雪崩 vs 快取擊穿

| | 快取擊穿 (Stampede) | 快取雪崩 (Avalanche) |
|---|---|---|
| 觸發原因 | **單一熱點 key** 過期 | **大量 key 同時**過期 |
| 影響規模 | 局部（一個 key） | 全面（整個快取層） |
| 典型情境 | 高流量單一資源 | 系統重啟、批次寫入快取 |

HybridCache 本身已內建 Stampede Protection（同一個 key 的並發請求只有一個會打到 DB），但雪崩需要額外策略才能防護。

---

## 專案結構

```
Lab.HybridCache.Avalanche/
├── Lab.HybridCache.Avalanche/
│   ├── Program.cs
│   ├── TtlJitterCacheService.cs
│   ├── LayeredTtlCacheService.cs
│   ├── CircuitBreakerCacheService.cs
│   ├── CacheWarmupService.cs
│   └── WeatherForecast.cs
└── Lab.HybridCache.Avalanche.IntegrationTests/
    └── CacheAvalancheIntegrationTests.cs
```

---

## 套件安裝

安裝 HybridCache、Redis 支援與韌性套件：

```bash
dotnet add package Microsoft.Extensions.Caching.Hybrid
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package Microsoft.Extensions.Http.Resilience
dotnet add package Polly.Extensions
```

---

## Program.cs — 服務註冊

把 Redis 設為 L2、啟用 HybridCache（L1 + L2），並注入三個策略服務與預熱服務：

```csharp
// 設定 Redis 作為 L2 快取
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 註冊 HybridCache 服務（L1 + L2 快取）
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(30),
        LocalCacheExpiration = TimeSpan.FromSeconds(10)
    };
});

builder.Services.AddSingleton<TtlJitterCacheService>();
builder.Services.AddSingleton<LayeredTtlCacheService>();
builder.Services.AddSingleton<CircuitBreakerCacheService>();
builder.Services.AddHostedService<CacheWarmupService>();
```

---

## 策略一：TTL Jitter（隨機抖動）

### 問題

批次寫入 100 個 key，TTL 全部設 10 分鐘，10 分鐘後 100 個 key 集體失效，瞬間 100 個請求打到 DB，雪崩就這樣發生了。

### 解法

寫入 key 時，在基礎 TTL 上加一段隨機時間，讓各 key 的失效時間錯開。

### 實作

`TtlJitterCacheService.cs` — 每次 `GetAsync()` 都會先重新計算隨機 Jitter，再動態建立 `HybridCacheEntryOptions`。這樣每次寫入快取時的 TTL 都不同，不會在 DI 階段就被固定住：

```csharp
public async Task<(WeatherForecast[] Data, string Source, TimeSpan L2Ttl)> GetAsync(
    string key, CancellationToken ct = default)
{
    var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 120));
    var l2Ttl = TimeSpan.FromMinutes(10) + jitter;
    var l1Ttl = TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(Random.Shared.Next(0, 30));

    var options = new HybridCacheEntryOptions
    {
        Expiration = l2Ttl,
        LocalCacheExpiration = l1Ttl
    };

    var source = "L1/L2";
    var data = await cache.GetOrCreateAsync(
        key,
        async token =>
        {
            source = "DB";
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss.fff}] [TTL Jitter] 從 DB 載入資料，key={key}，L2 TTL={l2Ttl.TotalSeconds:F0}s");
            await Task.Delay(5, token);
            return Enumerable.Range(1, 5).Select(i => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        },
        options,
        cancellationToken: ct);

    return (data!, source, l2Ttl);
}
```

端點回傳 `L2TtlSeconds` 欄位，呼叫多次就能觀察到每個 key 的 TTL 都不同。

---

## 策略二：分層 TTL（Layered TTL）

### 問題

L1（記憶體）跟 L2（Redis）TTL 設一樣，L1 過期的瞬間，L2 也剛好過期，請求直接穿透到 DB。

### 解法

L1 TTL 設比 L2 短，L1 miss 時 L2 仍有值可回傳，factory（打 DB）就不會被觸發。這是最輕量的防護方式。

### 實作

`LayeredTtlCacheService.cs` — L1 = 3 分鐘，L2 = 30 分鐘，設定一次就好：

```csharp
private static readonly HybridCacheEntryOptions Options = new()
{
    Expiration = TimeSpan.FromMinutes(30),         // L2 TTL
    LocalCacheExpiration = TimeSpan.FromMinutes(3) // L1 TTL，必須 < L2
};

public async Task<(WeatherForecast[] Data, string Source)> GetAsync(
    string key, CancellationToken ct = default)
{
    string source = "L1/L2";
    var data = await cache.GetOrCreateAsync(
        key,
        async token =>
        {
            source = "DB";
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Layered TTL] 從 DB 載入資料，key={key}，L1=3m，L2=30m");
            await Task.Delay(5, token);
            return Enumerable.Range(1, 5).Select(i => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        },
        Options,
        cancellationToken: ct);

    return (data!, source);
}
```

回傳的 `Source` 欄位在第一次呼叫時是 `"DB"`，之後只要 L2 還沒過期就是 `"L1/L2"`。

---

## 策略三：Circuit Breaker（斷路器）

### 問題

Redis 整層當機，HybridCache 的 L2 操作全部拋例外，每個請求都 fallthrough 到 DB，雪崩就發生了。

### 解法

用 Polly 的 ResiliencePipeline 包住 HybridCache 呼叫，失敗率超過 50%（取樣 10 秒，最少 3 次）就熔斷 30 秒，直接回傳降級資料（空陣列），不讓流量打到 DB。

### 實作

`CircuitBreakerCacheService.cs` — 建立 ResiliencePipeline，設定 OnOpened / OnClosed 事件，捕捉 `BrokenCircuitException` 回傳降級：

```csharp
private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 3,
        SamplingDuration = TimeSpan.FromSeconds(10),
        BreakDuration = TimeSpan.FromSeconds(30),
        OnOpened = args =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 熔斷觸發，暫停 30 秒");
            return ValueTask.CompletedTask;
        },
        OnClosed = args =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 熔斷關閉，恢復正常");
            return ValueTask.CompletedTask;
        }
    })
    .Build();

public async Task<(WeatherForecast[]? Data, string Source, bool IsFallback)> GetAsync(
    string key, CancellationToken ct = default)
{
    try
    {
        WeatherForecast[]? result = null;
        string source = "L1/L2";

        await _pipeline.ExecuteAsync(async token =>
        {
            result = await cache.GetOrCreateAsync(key, async innerToken =>
            {
                source = "DB";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 從 DB 載入資料，key={key}");
                await Task.Delay(5, innerToken);
                return Enumerable.Range(1, 5).Select(i => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                }).ToArray();
            }, Options, cancellationToken: token);
        }, ct);

        return (result, source, false);
    }
    catch (BrokenCircuitException)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 熔斷中，回傳降級資料");
        return ([], "Fallback", true);
    }
}
```

回傳的 `IsFallback` 欄位可讓呼叫端知道目前是降級狀態。

---

## 預熱服務：CacheWarmupService

服務重啟之後 L1/L2 全空，這就是冷啟動雪崩。解法是在 `IHostedService.StartAsync` 時主動把熱點 key 填進快取，讓流量進來的時候已經有資料可以回傳。

`CacheWarmupService.cs` — 啟動時預熱，失敗只 log warning，不影響服務啟動：

```csharp
public class CacheWarmupService(
    Microsoft.Extensions.Caching.Hybrid.HybridCache cache,
    ILogger<CacheWarmupService> logger) : IHostedService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private static readonly string[] HotKeys =
    [
        "weather-forecast-taipei",
        "weather-forecast-tokyo"
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var key in HotKeys)
        {
            try
            {
                await cache.GetOrCreateAsync(key, async ct =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Warmup] 預熱 key={key}");
                    await Task.Delay(5, ct);
                    return Enumerable.Range(1, 5).Select(i => new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    }).ToArray();
                }, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Warmup] 預熱 key={Key} 失敗，跳過", key);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

這個預熱點不是每次 request 都會觸發，而是在應用程式啟動時由 `IHostedService.StartAsync()` 呼叫一次。典型情境像是重新部署、手動重啟、容器重建、IIS/App Service 回收，或是程式 crash 後被平台重新拉起來。

不過，有 HybridCache 不代表一定要做預熱。HybridCache 已經能處理同一個 key 的並發擊穿；預熱則是額外處理「剛啟動時 L1/L2 都是空的」這個冷啟動窗口。所以如果流量平穩、資料源撐得住第一波穿透，其實可以不做。

真正需要預熱的，通常不是把所有重要 key 全部掃過一遍，而是只挑少量 **hot keys**。優先順序通常是：

1. 重啟後最先被打到的 key
2. 命中率高的熱門 key
3. 重建成本高、查詢慢的 key

預熱名單應該小而精；其餘資料交給自然流量慢慢建立即可，不然反而會把啟動時間拉長，甚至在啟動瞬間自己製造一波壓力。

---

## 整合測試

快取策略的核心行為只有搭配真實 Redis 才能有意義地驗證，這裡用 Testcontainers.Redis 在測試跑起來時自動起 Redis 容器（不需要手動設環境），測完自動清除。

### 測試初始化

`CacheAvalancheIntegrationTests.cs` — 使用 `IAsyncLifetime` 在測試前後管理容器生命週期：

```csharp
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
}
```

### 測試一：TTL Jitter 分散效果

寫入 20 個不同的 key，驗證 TTL 不會全部相同（代表 Jitter 有效果）：

```csharp
[Fact]
public async Task TtlJitter_MultipleKeys_ShouldHaveDifferentTtls()
{
    var svc = _sp.GetRequiredService<TtlJitterCacheService>();
    var ttls = new HashSet<int>();

    for (int i = 0; i < 20; i++)
    {
        var (_, _, l2Ttl) = await svc.GetAsync($"jitter-test-{i}");
        ttls.Add((int)l2Ttl.TotalSeconds);
    }

    Assert.True(ttls.Count > 1, $"Expected multiple distinct TTLs but got {ttls.Count}");
}
```

### 測試二：分層 TTL 行為驗證

第一次呼叫 factory 必定走 DB，第二次呼叫命中快取：

```csharp
[Fact]
public async Task LayeredTtl_L1TtlShouldBeLessThanL2Ttl()
{
    var svc = _sp.GetRequiredService<LayeredTtlCacheService>();
    var (data, source) = await svc.GetAsync("layered-test");

    Assert.NotNull(data);
    Assert.NotEmpty(data);
    Assert.Equal("DB", source); // 第一次必定是 DB
}

[Fact]
public async Task LayeredTtl_SecondCall_ShouldHitCache()
{
    var svc = _sp.GetRequiredService<LayeredTtlCacheService>();
    await svc.GetAsync("layered-hit-test");                  // 第一次寫入

    var (data, source) = await svc.GetAsync("layered-hit-test"); // 第二次命中快取
    Assert.Equal("L1/L2", source);
}
```

### 測試三：Circuit Breaker 正常運作

Redis 正常時，回傳真實資料，`IsFallback` 應為 false：

```csharp
[Fact]
public async Task CircuitBreaker_NormalOperation_ShouldReturnData()
{
    var svc = _sp.GetRequiredService<CircuitBreakerCacheService>();
    var (data, source, isFallback) = await svc.GetAsync("cb-test");

    Assert.NotNull(data);
    Assert.False(isFallback);
}
```

### 測試四：Stampede Protection 並發保護

10 個並發請求打同一個 key，HybridCache 保證 factory 只被呼叫一次：

```csharp
[Fact]
public async Task StampedeProtection_ConcurrentRequests_FactoryShouldBeCalledOnce()
{
    var cache = _sp.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
    int factoryCallCount = 0;
    var key = $"stampede-test-{Guid.NewGuid():N}";

    var tasks = Enumerable.Range(0, 10).Select(_ =>
        cache.GetOrCreateAsync(key, async ct =>
        {
            Interlocked.Increment(ref factoryCallCount);
            await Task.Delay(50, ct);
            return new[] { "data" };
        }).AsTask());

    await Task.WhenAll(tasks);

    Assert.Equal(1, factoryCallCount);
}
```

這個測試同時驗證了 HybridCache 的 Stampede Protection，跟本文主題快取雪崩的並發場景完全一致，一石二鳥 XDD

---

## 三策略對比

| 策略 | 防護目標 | 額外依賴 | 複雜度 |
|------|---------|---------|--------|
| TTL Jitter | 批次 key 集體失效 | 無 | 低 |
| 分層 TTL | L1/L2 同時失效 | 無 | 低 |
| Circuit Breaker | Redis 整層故障 | Polly | 中 |
| Cache Warmup | 冷啟動雪崩 | IHostedService | 低 |

---

## 心得

- TTL Jitter 與 分層 TTL 不需要額外套件，改幾行參數就搞定，是最值得直接套用的防護手段。
- Circuit Breaker 要搭 Polly，設定上要留意 `MinimumThroughput`，不然低流量時一兩次失敗就熔斷了，反而造成問題。
- HybridCache 的 Stampede Protection 是內建的，不需要自己用 `lock` 或 `SemaphoreSlim` 管，這是 HybridCache 比 `IMemoryCache` 好用的地方之一。
- Testcontainers.Redis 在 CI 環境下非常方便，不需要另外維護 Redis 測試環境。

---

## 範例位置
https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.HybridCache.Avalanche
