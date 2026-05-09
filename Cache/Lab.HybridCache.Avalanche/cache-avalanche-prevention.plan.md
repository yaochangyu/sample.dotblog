# Cache Avalanche Prevention — 實作計畫

## 目標

在現有 `Lab.HybridCache` 專案（.NET 10 + HybridCache + Redis）中，
新增三種防止快取雪崩（Cache Avalanche）的策略端點，
供後續部落格文章示範與對比。

## 快取雪崩 vs 快取擊穿

| | 快取擊穿 (Stampede) | 快取雪崩 (Avalanche) |
|---|---|---|
| 觸發原因 | **單一熱點 key** 過期 | **大量 key 同時**過期 |
| 影響規模 | 局部（一個 key） | 全面（整個快取層） |
| 典型情境 | 高流量單一資源 | 系統重啟、批次寫入快取 |

## 現有端點

| 端點 | 說明 |
|------|------|
| `GET /weatherforecast` | 無快取，每次直打資料源 |
| `GET /weatherforecast/hybrid-cache` | HybridCache 基本用法（L1+L2）|

## 新增端點

| 端點 | 策略 |
|------|------|
| `GET /weatherforecast/ttl-jitter` | 策略一：TTL Jitter（隨機抖動） |
| `GET /weatherforecast/layered-ttl` | 策略二：分層 TTL（L1 < L2 錯開） |
| `GET /weatherforecast/circuit-breaker` | 策略三：Circuit Breaker（Redis 熔斷）|

---

## 步驟

- [ ] **步驟 0：升級專案至 .NET 10**
  - 為何需要：將 `TargetFramework` 從 `net9.0` 改為 `net10.0`，並更新所有套件版本至 .NET 10 對應版本。
  - 修改 `Lab.HybridCache.Avalanche.csproj`

- [ ] **步驟 1：新增 `Polly` 套件**
  - 為何需要：策略三的 Circuit Breaker 使用 Polly 的 ResiliencePipeline，是 .NET 生態系標準的韌性套件。
  - 指令：`dotnet add package Polly.Extensions`、`dotnet add package Microsoft.Extensions.Http.Resilience`

- [ ] **步驟 2：實作策略一 — TTL Jitter (`/weatherforecast/ttl-jitter`)**
  - 為何需要：批次寫入 key 時若 TTL 相同，會在同一時間集體失效造成雪崩。加入隨機抖動讓失效時間分散。
  - 核心邏輯：`BaseTtl + Random.Shared.Next(0, 120) 秒`
  - 新增 `TtlJitterCacheService.cs`
  - 在 `Program.cs` 新增端點並注入服務

- [ ] **步驟 3：實作策略二 — 分層 TTL (`/weatherforecast/layered-ttl`)**
  - 為何需要：L1 TTL < L2 TTL，L1 miss 時 L2 仍有值可回傳，DB 幾乎零壓力，是最輕量的雪崩防護。
  - 設定：`LocalCacheExpiration = 3 min`、`Expiration = 30 min`
  - 新增 `LayeredTtlCacheService.cs`

- [ ] **步驟 4：實作策略三 — Circuit Breaker (`/weatherforecast/circuit-breaker`)**
  - 為何需要：Redis 整層當機時，若不熔斷，所有流量會直接打到 DB 導致雪崩。Circuit Breaker 在失敗率過高時自動切斷，返回降級結果。
  - 設定：失敗率 > 50% 且取樣 10 秒 → 熔斷 30 秒
  - 新增 `CircuitBreakerCacheService.cs`

- [ ] **步驟 5：新增 `CacheWarmupService` (IHostedService)**
  - 為何需要：服務重啟後 L1/L2 全空，冷啟動時大量請求直打 DB。預熱服務在啟動時主動填充熱點 key。
  - 新增 `CacheWarmupService.cs`，在 `StartAsync` 預熱指定熱點 key

- [ ] **步驟 6：Build 並確認編譯無誤**
  - 為何需要：確保所有新增程式碼語法正確、套件相依正確。
  - 指令：`dotnet build`

- [ ] **步驟 7：新增整合測試專案**
  - 為何需要：快取策略的核心行為（TTL 分散、L1/L2 回退、熔斷降級）只有搭配真實 Redis 才能有意義地驗證，單元測試無法覆蓋。
  - 工具：xUnit + Testcontainers.Redis（在 CI 中自動起 Redis 容器）
  - 測試案例：
    - **TTL Jitter**：寫入 100 個 key 後，從 Redis 取得各自的 TTL，驗證值不全相同（有分散效果）
    - **Layered TTL**：確認 L1 TTL < L2 TTL；L1 過期後請求命中 L2，不觸發 factory（DB 呼叫次數為 0）
    - **Circuit Breaker**：StopAsync() 停掉 Redis 容器，模擬 Redis 斷線；連續打超過失敗門檻後驗證回傳降級資料而非例外
    - **並發模擬**：同一 key 同時送出 N 個並發請求，驗證 factory（DB）只被呼叫一次（HybridCache stampede protection）
  - 新增測試專案 `Lab.HybridCache.Avalanche.IntegrationTests/`

- [ ] **步驟 8：更新 `CLAUDE.md`，補充雪崩防護策略說明**
  - 為何需要：讓 CLAUDE.md 反映最新的專案功能。

---

## 核心邏輯速查

### TTL Jitter
```csharp
TimeSpan jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 120));
var options = new HybridCacheEntryOptions
{
    Expiration = TimeSpan.FromMinutes(10) + jitter,
    LocalCacheExpiration = TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(Random.Shared.Next(0, 30))
};
```

### Circuit Breaker（Polly）
```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        BreakDuration = TimeSpan.FromSeconds(30)
    })
    .Build();
```

### Cache Warming
```csharp
public class CacheWarmupService(HybridCache cache) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hotKeys = new[] { "weather-forecast-taipei", "weather-forecast-tokyo" };
        foreach (var key in hotKeys)
            await cache.GetOrCreateAsync(key, async ct => await LoadFromDb(key, ct), cancellationToken: cancellationToken);
    }
}
```

---

## 注意事項

- 所有新端點的 Console log 格式統一：`[HH:mm:ss.fff] [策略名稱] 訊息`
- 每個端點回傳 `Source` 欄位說明資料來源（L1 / L2 / DB / Fallback）
- Circuit Breaker 熔斷時回傳降級資料（空陣列 + 提示訊息），不拋例外
- CacheWarmupService 失敗不影響服務啟動（try/catch + log warning）
