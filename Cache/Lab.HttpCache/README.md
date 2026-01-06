# Lab.HttpCache - HybridCache 快取實作範例

這是一個使用 .NET 9 **HybridCache** 的完整快取實作範例，展示現代化的快取機制。

## 專案架構

```
Lab.HttpCache/
├── src/
│   └── Lab.HttpCache.Api/
│       ├── Controllers/
│       │   └── CacheController.cs          # 快取範例 API
│       ├── Services/
│       │   ├── ICacheService.cs            # 快取服務介面
│       │   └── HybridCacheService.cs       # HybridCache 封裝服務
│       ├── Program.cs                      # 應用程式進入點
│       └── appsettings.json                # 配置檔
├── docker-compose.yml                      # Docker Compose 配置
└── README.md                               # 說明文件
```

## 快取機制

### 1. HybridCache (.NET 9 新功能) ⭐

**HybridCache** 是 .NET 9 引入的新快取抽象，自動整合 L1 (記憶體) 和 L2 (分散式) 快取。

#### 主要特性：

- **🚀 自動二級快取**：自動管理 L1 (Memory) 和 L2 (Redis) 快取
- **🛡️ Stampede Protection**：防止快取穿透 (Cache Stampede)
- **📦 自動序列化**：自動處理複雜物件的序列化/反序列化
- **🏷️ 標籤式失效**：支援基於標籤的快取失效
- **⚡ 更好的效能**：比傳統的 IMemoryCache + IDistributedCache 更高效

#### 運作原理：

```
請求 → L1 (Memory Cache)
         ├─ 命中 → 立即回傳 ⚡
         └─ 未命中 → L2 (Redis Cache)
                      ├─ 命中 → 回寫 L1 → 回傳
                      └─ 未命中 → 執行 Factory → 寫入 L1 & L2 → 回傳
```

#### HybridCache vs 傳統方式：

| 功能 | HybridCache | 傳統方式 (IMemoryCache + IDistributedCache) |
|------|-------------|---------------------------------------------|
| 二級快取 | ✅ 自動處理 | ❌ 需手動實作 |
| Stampede Protection | ✅ 內建 | ❌ 需手動實作 |
| 序列化 | ✅ 自動 | ❌ 需手動序列化 |
| 程式碼複雜度 | 🟢 簡單 | 🔴 複雜 |
| 效能 | 🟢 優化過 | 🟡 取決於實作 |

### 2. HTTP Cache (客戶端快取)

使用 HTTP 標頭控制瀏覽器或 CDN 快取：
- **ResponseCache Attribute**：使用屬性設定快取策略
- **Cache-Control**：手動設定快取控制標頭
- **ETag**：使用實體標籤進行條件請求

## 環境需求

- .NET 9.0 SDK
- Docker (用於執行 Redis)

## 快速開始

### 1. 啟動 Redis 服務

```bash
docker-compose up -d
```

### 2. 執行應用程式

```bash
cd src/Lab.HttpCache.Api
dotnet run
```

應用程式預設會在 `http://localhost:5000` 和 `https://localhost:5001` 啟動。

### 3. 存取 API 文件

開啟瀏覽器存取：
- OpenAPI 文件: `https://localhost:5001/openapi/v1.json`

## API 端點

### HybridCache 範例

#### 基本使用
```bash
# 取得或建立快取資料 (使用封裝服務)
GET /api/cache/hybrid?key=test1

# 第一次請求：執行 factory，寫入 L1 & L2
# 第二次請求：從 L1 (Memory) 讀取 ⚡
# L1 過期後：從 L2 (Redis) 讀取並回寫 L1
```

#### 直接使用 HybridCache
```bash
# 使用不同的 L1 和 L2 過期時間
GET /api/cache/hybrid-direct?key=test2

# L1 快取 2 分鐘，L2 快取 10 分鐘
```

#### 複雜物件快取
```bash
# 快取複雜物件 (自動序列化)
GET /api/cache/hybrid-complex?userId=user-123
```

#### 手動設定快取
```bash
# POST 設定快取值
POST /api/cache/hybrid-set?key=mykey
Content-Type: application/json

"my custom value"
```

#### 刪除快取
```bash
# 刪除單一快取 (同時清除 L1 和 L2)
DELETE /api/cache/hybrid/test1

# 透過標籤批量刪除快取
DELETE /api/cache/hybrid/tag/demo
# 這會刪除所有帶有 "demo" 標籤的快取項目
```

#### 快取統計資訊
```bash
# 取得 HybridCache 功能說明
GET /api/cache/stats
```

### HTTP Cache 範例

#### ResponseCache Attribute
```bash
# 使用 ResponseCache 屬性 (60 秒快取)
GET /api/cache/http-response-cache
```

#### Cache-Control 標頭
```bash
# 手動設定 Cache-Control (120 秒快取)
GET /api/cache/http-cache-control
```

#### ETag
```bash
# 使用 ETag 進行條件請求
GET /api/cache/http-etag

# 第二次請求時會回傳 304 Not Modified
```

## 測試快取機制

### 測試 HybridCache

```bash
# 第一次請求 - 快取未命中，執行 factory
curl http://localhost:5000/api/cache/hybrid?key=demo
# 回應時間: ~100ms (模擬資料庫查詢)

# 第二次請求 - L1 快取命中
curl http://localhost:5000/api/cache/hybrid?key=demo
# 回應時間: <1ms ⚡

# 測試複雜物件
curl http://localhost:5000/api/cache/hybrid-complex?userId=user-456

# 刪除快取
curl -X DELETE http://localhost:5000/api/cache/hybrid/demo
```

### 測試 HybridCache 的 Stampede Protection

```bash
# 同時發送多個相同請求，只會執行一次 factory
for i in {1..10}; do
  curl http://localhost:5000/api/cache/hybrid?key=stampede-test &
done
wait

# 檢查伺服器日誌，factory 只執行一次
```

### 測試不同的過期時間

```bash
# L1: 2 分鐘, L2: 10 分鐘
curl http://localhost:5000/api/cache/hybrid-direct?key=expiration-test

# 2 分鐘後再次請求 - 從 L2 讀取並回寫 L1
# 10 分鐘後再次請求 - 重新執行 factory
```

### 測試 HTTP Cache (使用 curl)

```bash
# 第一次請求 - 取得 ETag
curl -i http://localhost:5000/api/cache/http-etag

# 第二次請求 - 使用 ETag 條件請求
curl -i -H "If-None-Match: \"<ETag值>\"" http://localhost:5000/api/cache/http-etag
# 應該回傳 304 Not Modified
```

## 配置說明

### appsettings.json

```json
{
  "Redis": {
    "Configuration": "localhost:6379"
  }
}
```

### Program.cs - HybridCache 配置

```csharp
// 註冊 Redis 作為 L2 快取
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
});

// 註冊 HybridCache
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),        // L2 (Redis) 過期時間
        LocalCacheExpiration = TimeSpan.FromMinutes(1) // L1 (Memory) 過期時間 - 應比 L2 短
    };
});
```

> **重要提示：** 根據最佳實踐，L1 快取時間應該比 L2 短，這樣可以確保分散式環境中的資料一致性。

## HybridCache 使用範例

### 基本用法

```csharp
public class MyService
{
    private readonly HybridCache _cache;

    public MyService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<UserData> GetUserAsync(string userId)
    {
        return await _cache.GetOrCreateAsync(
            $"user:{userId}",
            async cancellationToken =>
            {
                // 從資料庫查詢
                return await _database.GetUserAsync(userId, cancellationToken);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),        // L2 快取 10 分鐘
                LocalCacheExpiration = TimeSpan.FromMinutes(2) // L1 快取 2 分鐘 (應比 L2 短)
            },
            tags: ["user-data", $"user:{userId}"]);  // 使用標籤便於批量清除
    }
}
```

### 使用 Tags 標籤進行快取管理

```csharp
// 建立帶有標籤的快取
await _cache.GetOrCreateAsync(
    "product:123",
    async ct => await GetProductAsync("123", ct),
    options,
    tags: ["product", "category:electronics"]);

await _cache.GetOrCreateAsync(
    "product:456",
    async ct => await GetProductAsync("456", ct),
    options,
    tags: ["product", "category:electronics"]);

// 批量清除所有電子產品相關的快取
await _cache.RemoveByTagAsync("category:electronics");
// 這會同時清除 product:123 和 product:456
```

### 手動設定快取

```csharp
// 設定快取
await _cache.SetAsync(
    "mykey",
    myValue,
    new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromHours(1)
    },
    tags: ["my-tag"]);

// 移除單一快取
await _cache.RemoveAsync("mykey");

// 透過標籤批量移除
await _cache.RemoveByTagAsync("my-tag");
```

### Tags 標籤使用最佳實踐

1. **為所有快取項目添加標籤**：便於批量管理和清除
   ```csharp
   tags: ["user-data", $"user:{userId}"]
   ```

2. **使用階層式標籤**：支援更細緻的快取控制
   ```csharp
   tags: ["product", "category:electronics", "brand:apple"]
   ```

3. **清除策略**：
   - 當使用者資料更新時：`RemoveByTagAsync($"user:{userId}")`
   - 當某個分類的產品更新時：`RemoveByTagAsync("category:electronics")`
   - 清除所有產品快取：`RemoveByTagAsync("product")`

## 快取過期時間建議

| 快取類型 | L1 (Memory) | L2 (Redis) | 說明 |
|---------|-------------|------------|------|
| 熱門資料 | 2-5 分鐘 | 10-30 分鐘 | 經常存取的資料 |
| 一般資料 | 5-10 分鐘 | 30-60 分鐘 | 中等頻率存取 |
| 靜態資料 | 10-30 分鐘 | 1-24 小時 | 很少變動的資料 |

## 技術堆疊

- **.NET 9.0**
- **ASP.NET Core Web API**
- **HybridCache** (Microsoft.Extensions.Caching.Hybrid)
- **Redis 7 Alpine** (作為 L2 分散式快取)
- **ResponseCache Middleware**

## HybridCache 優勢

### 1. 自動二級快取管理
不需要手動處理 L1/L2 快取邏輯，HybridCache 自動管理。

### 2. Stampede Protection
當多個請求同時查詢相同的快取鍵時，只會執行一次資料載入，其他請求等待結果。

**傳統方式的問題：**
```csharp
// ❌ 10 個請求同時進來，會執行 10 次資料庫查詢
var value = cache.Get(key);
if (value == null)
{
    value = await database.GetAsync(key); // 執行 10 次！
    cache.Set(key, value);
}
```

**HybridCache 解決方案：**
```csharp
// ✅ 10 個請求同時進來，只執行 1 次資料庫查詢
var value = await cache.GetOrCreateAsync(key, async ct =>
{
    return await database.GetAsync(key); // 只執行 1 次！
});
```

### 3. 自動序列化
HybridCache 自動處理複雜物件的序列化，不需要手動轉換。

### 4. 更好的效能
經過最佳化的實作，比手動組合 IMemoryCache + IDistributedCache 更高效。

## 注意事項

1. **Redis 連線**：確保 Redis 服務正在執行
2. **序列化**：HybridCache 使用的物件必須可序列化
3. **快取鍵命名**：建議使用有意義的前綴，如 `user:{id}`、`product:{id}`
4. **過期時間**：L1 過期時間應該 < L2 過期時間（最佳實踐）
5. **Tags 標籤**：為所有快取項目添加標籤，便於批量清除和管理

## 停止服務

```bash
# 停止應用程式
Ctrl+C

# 停止 Redis 容器
docker-compose down

# 停止並移除資料
docker-compose down -v
```

## 延伸閱讀

- [HybridCache 官方文件](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
- [.NET 9 新功能](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [ASP.NET Core 快取](https://learn.microsoft.com/zh-tw/aspnet/core/performance/caching/overview)
- [HTTP 快取標頭](https://developer.mozilla.org/zh-TW/docs/Web/HTTP/Caching)

## 從傳統快取遷移到 HybridCache

如果您目前使用 IMemoryCache + IDistributedCache，可以輕鬆遷移到 HybridCache：

**舊的方式：**
```csharp
// 複雜的手動二級快取邏輯
var value = _memoryCache.Get(key);
if (value == null)
{
    var bytes = await _distributedCache.GetAsync(key);
    if (bytes != null)
    {
        value = JsonSerializer.Deserialize<T>(bytes);
        _memoryCache.Set(key, value);
    }
    else
    {
        value = await GetFromDatabase(key);
        var json = JsonSerializer.Serialize(value);
        await _distributedCache.SetAsync(key, Encoding.UTF8.GetBytes(json));
        _memoryCache.Set(key, value);
    }
}
```

**新的方式：**
```csharp
// 簡潔的 HybridCache 用法
var value = await _hybridCache.GetOrCreateAsync(
    key,
    async ct => await GetFromDatabase(key, ct));
```

**節省超過 90% 的程式碼！** 🎉
