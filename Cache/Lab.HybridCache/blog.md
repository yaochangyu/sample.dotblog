---
title: .NET 9 的混合式快取 HybridCache
featuredImageUrl: https://dotblogsfile.blob.core.windows.net/user/余小章/b433eb26-87ea-4f0e-a989-0d37567c37ca/1759157946.png.png
abstract: <p>在現代 Web 應用程式開發中，快取是提升應用程式效能不可或缺的技術。在過去，我們會使用 IMemoryCache 做記憶體快取，或者使用 IDistributedCache 做分散式快取。</p><p>.NET 9 引入了全新的 HybridCache，它結合了記憶體快取（L1）和分散式快取（L2）的優勢，讓我們能夠在同一個 API 中享受兩層快取的效能提升，同時還提供了快取擊穿保護和標籤管理等進階功能。</p><figure class="image"><img style="aspect-ratio:1024/1024;" src="https://dotblogsfile.blob.core.windows.net/user/余小章/b433eb26-87ea-4f0e-a989-0d37567c37ca/1759160006.png.png" width="1024" height="1024"></figure>
keywords: Cache,HybridCache
categories: Cache
weblogName: My Blog
postId: b433eb26-87ea-4f0e-a989-0d37567c37ca
postDate: 2025-09-29T14:50:42.0000000
postStatus: publish
dontInferFeaturedImage: false
stripH1Header: true
---
# .NET 9 的混合式快取 HybridCache

## 開發環境

* Windows 11 Pro
* ASP.NET Core 9.0
* Microsoft.Extensions.Caching.Hybrid
* Microsoft.Extensions.Caching.StackExchangeRedis
* Rider 2025.2

---

## HybridCache 是什麼？

HybridCache 是 .NET 9 新推出的快取程式庫，透過 `Microsoft.Extensions.Caching.Hybrid` 套件提供。所謂「混合快取」，就是同時管理兩層：

* **L1 快取**：記憶體內快取，存取速度最快，但只活在當前 process 裡
* **L2 快取**：分散式快取（Redis、SQL Server 等），多個 instance 共用，速度慢一點但可以跨 process 共享

查詢邏輯很直觀：L1 沒有 → 問 L2 → L2 也沒有 → 執行 factory 回源，結果同時寫入 L1 與 L2。

```plaintext
graph TB
    A[應用程式請求] --> B[HybridCache]
    B --> C[L1 記憶體快取]
    C --> D{命中?}
    D -->|是| E[直接回傳]
    D -->|否| F[L2 分散式快取]
    F --> G{命中?}
    G -->|是| H[回傳並存入L1]
    G -->|否| I[執行資料來源]
    I --> J[存入L1與L2]
    J --> K[回傳結果]
```

<figure class="image"><img style="aspect-ratio:757/2048;" src="https://dotblogsfile.blob.core.windows.net/user/余小章/b433eb26-87ea-4f0e-a989-0d37567c37ca/1759157946.png.png" width="757" height="2048"></figure>

除了雙層快取，HybridCache 還有幾個亮點：

* **避免快取擊穿 (Cache Breakdown)**：同一個 process 內，相同 key 的並發請求，只有第一個會執行 factory，其他請求等待結果，避免瞬間打爆資料庫
* **標籤式快取管理**：可以用標籤批次清除相關快取，不用一個 key 一個 key 刪
* **自訂序列化器**：預設 `System.Text.Json`，也可以換成 protobuf 或 XML

---

## 快取擊穿防護的邊界

這裡有一個很重要的細節，要特別說清楚。

HybridCache 的快取擊穿防護是 **process 內的 in-flight deduplication**。根據官方文件，快取資料儲存在 process 內（`each server has a separate cache`），因此防護範圍自然只在當前 process 內有效。

換句話說，如果你部署了 3 個 Pod，同一時間三個 Pod 都發生快取 miss：

```
Pod A → 執行 factory，打一次 DB
Pod B → 執行 factory，打一次 DB
Pod C → 執行 factory，打一次 DB
```

每個 Pod 各自保護自己 process 內的並發請求，但 Pod 之間沒有協調，結果還是會打 3 次 DB（而不是全部幾百個並發請求都打）。

**這是設計邊界，不是 bug。** 大多數情況下，3 次 vs 數百次已經差很多了，實際上通常可以接受。

若真的需要跨 process 的保護，常見做法：

1. **Redis Distributed Lock**（`RedLock`、`SETNX + TTL`）：在取資料前先搶鎖，複雜度比較高
2. **接受 N 次打穿**（N = instance 數量）：instance 少、DB 撐得住的情況下，直接接受即可

---

## 如何安裝？

安裝 HybridCache 套件：

```bash
dotnet add package Microsoft.Extensions.Caching.Hybrid
```

如果要用 Redis 當 L2 快取，再安裝：

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

---

## 基本設定

只使用 L1 記憶體快取（最簡單的設定）：

```cs
builder.Services.AddHybridCache();
```

加上 Redis 作為 L2 快取：

```cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),        // L2 過期時間
        LocalCacheExpiration = TimeSpan.FromMinutes(1) // L1 過期時間（應比 L2 短）
    };
});
```

> **NOTE：L1 快取時間應比 L2 短**，避免 L1 資料比 L2 還新鮮造成不一致。

---

## 快取資料的方式

在應用程式端注入 `Microsoft.Extensions.Caching.Hybrid.HybridCache`，然後呼叫 `GetOrCreateAsync`：

```cs
var weather = await _cache.GetOrCreateAsync(
    key: $"weather-{city}",
    factory: async token =>
    {
        await Task.Delay(1000, token); // 模擬外部 API
        return new WeatherInfo(city, 28, "晴朗", DateTime.UtcNow);
    },
    options: new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    },
    tags: [$"weather-{city}"]);
```

我的範例是在 Minimal API 直接注入使用，完整程式碼如下：

```cs
app.MapGet("/weatherforecast/hybrid-cache",
    async (Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache) =>
    {
        var forecast = await hybridCache.GetOrCreateAsync(
            key: "weather-forecast",
            factory: async cancellationToken =>
            {
                await Task.Delay(TimeSpan.FromMicroseconds(2), cancellationToken);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 從資料來源生成天氣預報資料");

                return Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = summaries[Random.Shared.Next(summaries.Length)]
                        })
                    .ToArray();
            },
            tags: ["weather-forecast"]
        );

        return Results.Ok(new
        {
            Data = forecast,
            CachedAt = DateTime.Now,
            Message = "資料來自 HybridCache (L1: 記憶體 + L2: Redis)"
        });
    })
    .WithName("GetWeatherForecastWithHybridCache")
    .WithSummary("使用 HybridCache 的天氣預報")
    .WithDescription("展示 HybridCache L1（記憶體）+ L2（Redis）雙層快取功能");
```

---

## 標籤式快取管理

HybridCache 支援標籤，可以批次清除相關快取，在產品更新、類別變動時非常實用：

```cs
// 清除特定產品的快取
await _cache.RemoveByTagAsync("product-123");

// 清除某個類別底下所有快取
await _cache.RemoveByTagAsync("category-electronics");
```

---

## 自訂序列化器

預設使用 `System.Text.Json`，如果要改用 protobuf，可以這樣設定：

```cs
options.WithSerializerFactory(type =>
{
    if (type == typeof(Product))
        return new ProtobufSerializer<Product>();
    return null;
});
```

---

## 心得

HybridCache 讓我們不再需要在記憶體快取和分散式快取之間二選一，一個 API 搞定兩層。

幾個要記住的使用原則：

* L1 過期時間要比 L2 短，避免資料不一致
* 所有快取都打上標籤，日後清除方便很多
* 快取擊穿防護只在 **單一 process 內有效**，多 instance 部署時，每個 instance 還是各自打一次 DB；若要跨 process 保護，需要額外搭配分散式鎖

---

## 參考資料

* [Microsoft Learn - HybridCache library in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
* [.NET Blog - HybridCache 官方介紹](https://devblogs.microsoft.com/dotnet/hybrid-cache-is-now-ga/)
* [Microsoft.Extensions.Caching.Hybrid NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Hybrid/)

---

## 範例位置

[sample.dotblog/Cache/Lab.HybridCache at master · yaochangyu/sample.dotblog](https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.HybridCache)