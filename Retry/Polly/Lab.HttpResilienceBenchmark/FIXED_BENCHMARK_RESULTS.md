# HttpResilienceBenchmark 效能測試結果分析報告

## 🔍 測試環境資訊
- **測試平台**: Linux Ubuntu 24.04.3 LTS
- **處理器**: Intel Core i9-14900HX 2.42GHz (16 實體核心, 32 邏輯核心)
- **框架**: .NET 9.0.4
- **測試工具**: BenchmarkDotNet v0.15.4
- **API 端點**: http://localhost:5068/api/members
- **測試時間**: 2025-09-25

## 📊 效能測試結果

| 方法 | 平均時間 | 誤差 | 標準差 | 相對比率 | 記憶體分配 | 分配比率 |
|-----|---------|------|--------|---------|-----------|----------|
| **StandardHttpClient** (基準) | 158.0 μs | ±3.13 μs | 6.68 μs | 1.00 | 3.31 KB | 1.00 |
| **PollyV8HttpClient** | 171.9 μs | ±3.42 μs | 8.32 μs | 1.09 | 4.95 KB | 1.49 |
| **ResilienceHttpClient** | 174.7 μs | ±3.47 μs | 8.46 μs | 1.11 | 6.48 KB | 1.96 |
| **PollyV7HttpClient** | 204.3 μs | ±11.53 μs | 33.64 μs | 1.30 | 4.46 KB | 1.35 |

## 📈 關鍵發現

### 1. **效能排序 (由快到慢)**
1. **StandardHttpClient**: 158.0 μs (基準)
2. **PollyV8HttpClient**: 171.9 μs (+8.8%)
3. **ResilienceHttpClient**: 174.7 μs (+10.6%)
4. **PollyV7HttpClient**: 204.3 μs (+29.3%)

### 2. **重要觀察**
- **Polly V8** 相比 **Polly V7** 有顯著效能提升 (15.9% 更快)
- **Microsoft.Extensions.Http.Resilience** 與 **Polly V8** 效能相近，差異僅 2.8%
- **Polly V7** 有較高的變異性 (標準差 33.64 μs vs 其他約 6-8 μs)
- 所有彈性方案都有額外的記憶體開銷

### 3. **記憶體使用分析**
- **StandardHttpClient**: 3.31 KB (最少)
- **PollyV7HttpClient**: 4.46 KB (+35%)
- **PollyV8HttpClient**: 4.95 KB (+49%)
- **ResilienceHttpClient**: 6.48 KB (+96% - 最多)

## 🚀 建議

### 1. **生產環境推薦**:
- 使用 **Polly V8** 或 **Microsoft.Extensions.Http.Resilience**
- 避免使用 **Polly V7** (效能最差且不穩定)

### 2. **選擇考量**:
- 若重視效能: **Polly V8**
- 若重視整合性: **Microsoft.Extensions.Http.Resilience** (官方整合)
- 若無彈性需求: **StandardHttpClient** (效能最佳)

### 3. **效能成本**:
- 彈性功能的額外開銷約 8-30% 執行時間
- 記憶體使用增加 35-96%

## 🔧 測試配置詳細資料

### StandardHttpClient
```csharp
services.AddHttpClient("standard", client =>
{
    client.BaseAddress = new Uri(_baseUrl);
});
```

### Microsoft.Extensions.Http.Resilience
```csharp
services.AddHttpClient("resilience", client =>
{
    client.BaseAddress = new Uri(_baseUrl);
})
.AddStandardResilienceHandler();
```

### Polly V8
```csharp
services.AddHttpClient("pollyV8", client =>
{
    client.BaseAddress = new Uri(_baseUrl);
})
.AddResilienceHandler("retry", builder =>
{
    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(response => !response.IsSuccessStatusCode)
            .Handle<HttpRequestException>()
    });
});
```

### Polly V7
```csharp
services.AddHttpClient("pollyV7", client =>
{
    client.BaseAddress = new Uri(_baseUrl);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

## 📄 原始測試數據

```
BenchmarkDotNet v0.15.4, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Core i9-14900HX 2.42GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.203
  [Host]     : .NET 9.0.4 (9.0.4, 9.0.425.16305), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 9.0.4 (9.0.4, 9.0.425.16305), X64 RyuJIT x86-64-v3

| Method               | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| StandardHttpClient   | 158.0 μs |  3.13 μs |  6.68 μs |  1.00 |    0.06 |      - |   3.31 KB |        1.00 |
| ResilienceHttpClient | 174.7 μs |  3.47 μs |  8.46 μs |  1.11 |    0.07 | 0.2441 |   6.48 KB |        1.96 |
| PollyV8HttpClient    | 171.9 μs |  3.42 μs |  8.32 μs |  1.09 |    0.07 | 0.2441 |   4.95 KB |        1.49 |
| PollyV7HttpClient    | 204.3 μs | 11.53 μs | 33.64 μs |  1.30 |    0.22 |      - |   4.46 KB |        1.35 |
```

## 🔍 結論

此次測試驗證了 **Polly V8** 相較於 **Polly V7** 在效能和穩定性方面的顯著改善。同時證明了 **Microsoft.Extensions.Http.Resilience** 作為官方解決方案提供了良好的彈性能力，雖然記憶體使用較高，但效能表現與 Polly V8 相近。

對於需要 HTTP 彈性功能的應用程式，建議優先考慮 **Polly V8** 或 **Microsoft.Extensions.Http.Resilience**，避免使用已過時的 **Polly V7**。