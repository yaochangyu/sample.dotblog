# Polly V8 完整介紹

## 🔍 Polly 是什麼？

**Polly** 是一個 .NET 的彈性和瞬時故障處理函式庫，提供：

- **重試機制** (Retry)
- **斷路器** (Circuit Breaker)
- **超時控制** (Timeout)
- **隔離** (Bulkhead)
- **快取** (Cache)
- **回退策略** (Fallback)

## 📈 Polly V8 的主要特色

### 1. **全新架構設計**
```csharp
// Polly V8 - 新的彈性管道
services.AddHttpClient("pollyV8")
.AddResilienceHandler("retry", builder =>
{
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1)
    });
});
```

### 2. **效能大幅提升**
根據測試報告顯示：
- 比 Polly V7 快 **15.9%**
- 記憶體使用更有效率
- 更低的變異性（標準差從 33.64μs 降到 8.32μs）

### 3. **更好的型別安全**
- 強型別的彈性策略
- 更清晰的 API 設計
- 更好的編譯時期檢查

## 🆚 版本比較

| 特色 | Polly V7 | Polly V8 |
|------|----------|----------|
| **效能** | 204.3μs | 171.9μs |
| **API 設計** | 舊式 Policy | 新式 Resilience Pipeline |
| **記憶體使用** | 4.46KB | 4.95KB |
| **穩定性** | 變異性高 | 變異性低 |
| **維護狀態** | 維護模式 | 主要開發版本 |

## 🎯 為什麼選擇 Polly V8？

1. **現代化架構**：全新設計的彈性管道
2. **更好效能**：相比 V7 有顯著提升
3. **長期支援**：微軟官方推薦的版本
4. **生態整合**：與 `Microsoft.Extensions.Http.Resilience` 相容

## 📚 使用範例

### 基本重試策略
```csharp
services.AddHttpClient("api")
.AddResilienceHandler("standard", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions())
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions())
        .AddTimeout(TimeSpan.FromSeconds(30));
});
```

### 進階配置範例
```csharp
services.AddHttpClient("advanced")
.AddResilienceHandler("comprehensive", builder =>
{
    // 重試策略
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(response => !response.IsSuccessStatusCode)
            .Handle<HttpRequestException>()
    });

    // 斷路器
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    // 超時控制
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

## 🏗️ 架構優勢

### Polly V7 架構
```csharp
// 舊式 Policy 鏈結
var retryPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
```

### Polly V8 架構
```csharp
// 新式 Resilience Pipeline
var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>())
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>())
    .Build();
```

## 📊 效能優勢詳細分析

根據 HttpResilienceBenchmark 測試結果：

### 執行時間比較
- **Polly V7**: 204.3μs ± 11.53μs
- **Polly V8**: 171.9μs ± 3.42μs
- **改善幅度**: 15.9% 更快

### 穩定性比較
- **Polly V7 標準差**: 33.64μs（變異性高）
- **Polly V8 標準差**: 8.32μs（變異性低）
- **改善幅度**: 75.3% 更穩定

### 記憶體使用
- **Polly V7**: 4.46KB
- **Polly V8**: 4.95KB
- **差異**: +10.9%（輕微增加，但效能大幅提升）

## 🚀 遷移建議

### 從 Polly V7 遷移到 V8

1. **更新套件參考**
```xml
<!-- 移除舊版本 -->
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />

<!-- 加入新版本 -->
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

2. **更新程式碼**
```csharp
// V7 方式
services.AddHttpClient("api")
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// V8 方式
services.AddHttpClient("api")
.AddResilienceHandler("retry", builder =>
{
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1)
    });
});
```

## 🔧 實用配置範本

### Web API 客戶端配置
```csharp
public static class HttpClientExtensions
{
    public static IServiceCollection AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        string baseUrl)
    {
        return services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddResilienceHandler("standard", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(1),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => !r.IsSuccessStatusCode)
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(30)
                })
                .AddTimeout(TimeSpan.FromSeconds(10));
        })
        .Services;
    }
}
```

## 📖 參考資源

- [Polly V8 官方文檔](https://www.pollydocs.org/)
- [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [GitHub Repository](https://github.com/App-vNext/Polly)

## 🎯 結論

Polly V8 是目前 .NET 生態系中處理 HTTP 彈性的最佳選擇之一！相比 V7 版本：

- ✅ **效能提升 15.9%**
- ✅ **穩定性提升 75.3%**
- ✅ **現代化 API 設計**
- ✅ **更好的型別安全**
- ✅ **官方長期支援**

對於新專案，強烈建議直接使用 Polly V8；對於現有專案，建議盡快規劃遷移至 V8 版本。