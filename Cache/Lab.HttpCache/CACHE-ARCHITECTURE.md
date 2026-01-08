# HTTP 快取多層架構說明

本專案展示了一個完整的多層快取架構，結合了**伺服器端快取**（HybridCache）和**客戶端快取**（HTTP Cache-Control + ETag），實現最佳的效能與資料一致性。

## 技術棧

- **後端框架**：ASP.NET Core 9.0
- **快取機制**：.NET 9 HybridCache
- **分散式快取**：Redis (StackExchange.Redis)
- **HTTP 規範**：RFC 9111（HTTP Caching）

## 架構概覽

```
┌──────────────────────────────────────────────────────────────────┐
│                          客戶端（瀏覽器）                          │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  L1: 瀏覽器快取（Browser Cache）                        │    │
│  │  - 使用 Cache-Control: max-age=60                      │    │
│  │  - 60 秒內完全不發送請求                                │    │
│  │  - ETag 用於過期後的驗證（304 Not Modified）            │    │
│  └────────────────────────────────────────────────────────┘    │
│                              ↓                                   │
└──────────────────────────────────────────────────────────────────┘
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│                      伺服器端（ASP.NET Core）                      │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  Controller Layer (ClientCacheController)              │    │
│  │  - 處理 HTTP 請求                                      │    │
│  │  - 設定 Cache-Control 標頭                            │    │
│  │  - 生成並驗證 ETag                                     │    │
│  │  - 回傳 304 Not Modified                              │    │
│  └────────────────────────────────────────────────────────┘    │
│                              ↓                                   │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  Repository Layer (ArticleRepository)                  │    │
│  │  - 使用 HybridCacheProvider 快取查詢結果               │    │
│  │  - 更新資料時清除相關快取                              │    │
│  └────────────────────────────────────────────────────────┘    │
│                              ↓                                   │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  L2: HybridCache (Memory + Redis)                      │    │
│  │  - L1 (Memory): 1 分鐘                                 │    │
│  │  - L2 (Redis): 5 分鐘                                  │    │
│  │  - 自動處理快取一致性                                   │    │
│  └────────────────────────────────────────────────────────┘    │
│                              ↓                                   │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  L3: Data Source (模擬資料庫)                           │    │
│  │  - 靜態 Dictionary<int, Article>                       │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

## 快取層級說明

### 第一層：瀏覽器快取（Browser Cache）
- **位置**：客戶端瀏覽器
- **快取時間**：60 秒（由 `Cache-Control: max-age=60` 控制）
- **優點**：
  - 完全不發送網路請求，速度最快
  - 節省網路流量
  - 降低伺服器負載
- **驗證機制**：
  - 過期後使用 ETag 進行條件請求
  - 如果內容未變更，伺服器回傳 304 Not Modified

### 第二層：HybridCache L1（記憶體快取）
- **位置**：伺服器記憶體
- **快取時間**：1 分鐘
- **優點**：
  - 極快的讀取速度（ns 級別）
  - 減少 Redis 網路延遲
  - 自動序列化/反序列化
- **適用場景**：高頻率讀取的資料

### 第三層：HybridCache L2（分散式快取 - Redis）
- **位置**：Redis 伺服器
- **快取時間**：5 分鐘
- **優點**：
  - 跨多個伺服器實例共享快取
  - 記憶體不足時不會遺失
  - 支援大容量快取
- **適用場景**：需要在多個伺服器間共享的資料

### 第四層：資料來源（Data Source）
- **位置**：資料庫（本專案使用靜態字典模擬）
- **特點**：
  - 最終的資料來源
  - 只有在所有快取層都未命中時才會存取

## 實際運作流程

### 情境一：首次請求文章

```
1. 瀏覽器發送: GET /api/clientcache/article/1
   ↓
2. Controller 收到請求
   ↓
3. Repository.GetArticle(1) 被呼叫
   ↓
4. HybridCache 檢查 L1 (Memory) → 未命中
   ↓
5. HybridCache 檢查 L2 (Redis) → 未命中
   ↓
6. 從資料來源讀取（模擬資料庫延遲 10ms）
   ↓
7. 將結果儲存到 L2 (Redis, 5分鐘) 和 L1 (Memory, 1分鐘)
   ↓
8. Controller 生成 ETag = "638734982345678901"
   ↓
9. 回傳 200 OK + 完整資料
   Headers: Cache-Control: public, max-age=60, must-revalidate
           ETag: "638734982345678901"
```

**總耗時**：~10ms（資料庫讀取）

### 情境二：60 秒內再次請求

```
1. 瀏覽器發送: GET /api/clientcache/article/1
   ↓
2. 瀏覽器檢查本地快取 → 命中（未過期）
   ↓
3. 直接使用快取，不發送請求到伺服器
```

**總耗時**：~0ms（完全不需要網路請求）

### 情境三：60 秒後再次請求（內容未改變）

```
1. 瀏覽器發送: GET /api/clientcache/article/1
   Headers: If-None-Match: "638734982345678901"
   ↓
2. Controller 收到條件請求
   ↓
3. Repository.GetArticle(1) 被呼叫
   ↓
4. HybridCache 檢查 L1 (Memory) → 命中！
   ↓
5. 直接回傳快取的文章（不需存取 Redis 或資料庫）
   ↓
6. Controller 比對 ETag → 相同
   ↓
7. 回傳 304 Not Modified（無 body）
```

**總耗時**：~1ms（只有記憶體存取）
**流量節省**：~99%（只傳輸標頭，無 body）

### 情境四：1 分鐘後但 5 分鐘內再次請求

```
1. 瀏覽器發送: GET /api/clientcache/article/1
   Headers: If-None-Match: "638734982345678901"
   ↓
2. Controller 收到條件請求
   ↓
3. Repository.GetArticle(1) 被呼叫
   ↓
4. HybridCache 檢查 L1 (Memory) → 過期
   ↓
5. HybridCache 檢查 L2 (Redis) → 命中！
   ↓
6. 從 Redis 讀取並更新 L1
   ↓
7. Controller 比對 ETag → 相同
   ↓
8. 回傳 304 Not Modified
```

**總耗時**：~5ms（Redis 網路延遲）

### 情境五：更新文章

```
1. 瀏覽器發送: PUT /api/clientcache/article/1
   Body: { "title": "新標題", "content": "新內容" }
   ↓
2. Controller 呼叫 Repository.UpdateArticle(1, ...)
   ↓
3. Repository 更新資料來源
   ↓
4. Repository 清除快取：
   - 移除 "article:1" 快取（L1 + L2）
   - 移除 "articles:all" 快取（L1 + L2）
   ↓
5. 更新 article.UpdatedAt = DateTime.UtcNow
   ↓
6. 生成新的 ETag = "638734992345678902"（已改變）
   ↓
7. 回傳 200 OK + 新的 ETag

下次請求時：
1. 瀏覽器發送: GET /api/clientcache/article/1
   Headers: If-None-Match: "638734982345678901"（舊的 ETag）
   ↓
2. HybridCache 未命中（已清除）
   ↓
3. 從資料來源讀取新資料
   ↓
4. Controller 比對 ETag → 不同
   ↓
5. 回傳 200 OK + 完整的新資料 + 新的 ETag
```

## 效能提升分析

假設一個文章 API：
- 完整回應大小：5 KB
- 每秒請求數：100 req/s
- 資料庫查詢時間：10ms

### 沒有快取

- 每秒流量：5 KB × 100 = 500 KB/s
- 資料庫查詢：100 req/s × 10ms = 1000ms = 1 個 CPU 核心滿載
- 回應時間：~10ms

### 使用完整的多層快取

假設：
- 60% 的請求在瀏覽器快取期間內（60秒內）
- 30% 的請求在伺服器快取期間內（使用 304 回傳）
- 10% 的請求需要完整回應

**流量節省：**
- 瀏覽器快取：60% × 0 KB = 0 KB
- 304 回應：30% × 0.2 KB = 0.06 KB
- 完整回應：10% × 5 KB = 0.5 KB
- 總流量：0.56 KB/s（節省 **99.9%**）

**伺服器負載：**
- 完全不到達伺服器：60%
- 到達但使用 L1 快取（~1ms）：30%
- 使用 L2 快取（~5ms）：9%
- 查詢資料庫（~10ms）：1%
- 平均回應時間：0.6 × 0 + 0.3 × 1 + 0.09 × 5 + 0.01 × 10 = **0.85ms**

**資料庫負載：**
- 只有 1% 的請求需要查詢資料庫
- 從 100 req/s 降至 **1 req/s**（降低 **99%**）

## 快取一致性策略

### 寫入時失效（Write-Invalidate）

當資料更新時，主動清除相關快取：

```csharp
public bool UpdateArticle(int id, string title, string content)
{
    if (!_articles.TryGetValue(id, out var article))
    {
        return false;
    }

    // 1. 更新資料
    article.Title = title;
    article.Content = content;
    article.UpdatedAt = DateTime.UtcNow; // 改變 ETag

    // 2. 清除相關快取
    var cacheKey = $"{ArticleCacheKeyPrefix}{id}";
    _cacheProvider.RemoveAsync(cacheKey).GetAwaiter().GetResult();
    _cacheProvider.RemoveAsync(AllArticlesCacheKey).GetAwaiter().GetResult();

    return true;
}
```

### ETag 驗證機制

即使伺服器端快取命中，客戶端仍可能使用舊的快取：

```csharp
// Controller 層
var etag = $"\"{article.UpdatedAt.Ticks}\"";
Response.Headers.ETag = etag;

if (Request.Headers.IfNoneMatch == etag)
{
    return StatusCode(304); // 內容未變更
}
```

這確保了即使多個快取層，資料的一致性仍然得到保證。

## 標籤（Tags）系統

HybridCache 支援使用標籤來批量清除快取：

```csharp
// 儲存時加上標籤（在 ArticleRepository.GetArticle 中）
var article = _cacheProvider.GetOrCreateAsync(
    key: $"article:{id}",
    factory: async ct => { /* 查詢邏輯 */ },
    expiration: TimeSpan.FromMinutes(5),
    tags: new[] { ArticleTag, $"{ArticleTag}:{id}" }  // "articles" 和 "articles:1"
);

// 清除所有包含特定標籤的快取
_cacheProvider.RemoveByTagAsync($"articles:{id}");
```

這在複雜的資料關聯場景中非常有用。

## 測試指令

### 測試文章端點

```bash
# 1. 首次請求（會從資料來源讀取，約 10ms）
curl -i http://localhost:5178/api/clientcache/article/1
# 注意回應中的 requestId 和 ETag

# 2. 立即再次請求（瀏覽器會使用快取，不會發送請求）
# 在瀏覽器中測試，或等待 60 秒後...

# 3. 使用 ETag 進行條件請求（會收到 304）
curl -i -H "If-None-Match: \"<your-etag>\"" \
  http://localhost:5178/api/clientcache/article/1

# 4. 更新文章（會清除快取）
curl -X PUT -H "Content-Type: application/json" \
  -d '{"title":"更新的標題","content":"更新的內容"}' \
  http://localhost:5178/api/clientcache/article/1

# 5. 再次請求（ETag 已改變，會收到新資料）
curl -i -H "If-None-Match: \"<old-etag>\"" \
  http://localhost:5178/api/clientcache/article/1
```

### 觀察快取行為

```bash
# 查看伺服器收到的總請求數
curl http://localhost:5178/api/clientcache/stats

# 重置計數器
curl -X POST http://localhost:5178/api/clientcache/reset
```

## 最佳實踐

1. **根據資料特性選擇快取策略**
   - 靜態資料（如歷史文章）：長時間快取 + immutable
   - 半靜態資料（如產品資訊）：中等時間快取 + ETag 驗證
   - 動態資料（如即時數據）：no-store 或極短時間快取

2. **合理設定快取時間**
   - 客戶端快取 (max-age) < 伺服器端 L1 < 伺服器端 L2
   - 本專案：60s < 1m < 5m

3. **使用 ETag 確保資料一致性**
   - 使用資料的最後更新時間或版本號生成 ETag
   - 總是處理 If-None-Match 條件請求

4. **更新時清除快取**
   - 更新資料後立即清除相關快取
   - 使用標籤系統批量清除相關快取

5. **監控快取命中率**
   - 使用 requestId 追蹤實際到達伺服器的請求數
   - 監控 304 vs 200 的比例

## 專案結構

```
Lab.HttpCache/
├── src/Lab.HttpCache.Api/
│   ├── Controllers/
│   │   ├── CacheController.cs              # HybridCache 基本範例
│   │   └── ClientCacheController.cs        # HTTP 客戶端快取實驗端點
│   ├── Models/
│   │   └── Article.cs                      # 文章模型
│   ├── Providers/
│   │   ├── ICacheProvider.cs               # 快取提供者介面
│   │   └── HybridCacheProvider.cs          # HybridCache 封裝
│   ├── Repositories/
│   │   ├── IArticleRepository.cs           # 文章倉儲介面
│   │   └── ArticleRepository.cs            # 文章倉儲實作（使用 HybridCache）
│   ├── OpenApiUiExtensions.cs              # API 文件擴展
│   └── Program.cs                          # 應用程式進入點
├── docker-compose.yml                      # Redis 容器配置
├── blog-article.md                         # 技術文章
└── CACHE-ARCHITECTURE.md                   # 本文件
```

## 關鍵程式碼說明

### ArticleRepository.cs

```csharp
public class ArticleRepository : IArticleRepository
{
    // 靜態字典模擬資料庫（所有實例共享）
    private static readonly Dictionary<int, Article> _articles;
    private readonly ICacheProvider _cacheProvider;

    // 快取鍵常數
    private const string ArticleCacheKeyPrefix = "article:";
    private const string AllArticlesCacheKey = "articles:all";
    private const string ArticleTag = "articles";

    public Article? GetArticle(int id)
    {
        var cacheKey = $"{ArticleCacheKeyPrefix}{id}";

        return _cacheProvider.GetOrCreateAsync(
            key: cacheKey,
            factory: async ct =>
            {
                await Task.Delay(10, ct); // 模擬資料庫延遲
                return _articles.TryGetValue(id, out var result) ? result : null;
            },
            expiration: TimeSpan.FromMinutes(5),
            tags: new[] { ArticleTag, $"{ArticleTag}:{id}" }
        ).GetAwaiter().GetResult();
    }
}
```

### ClientCacheController.cs

```csharp
[HttpGet("article/{id}")]
public IActionResult GetArticle(int id)
{
    var article = _repository.GetArticle(id);

    if (article == null)
    {
        return NotFound();
    }

    // 使用 UpdatedAt.Ticks 生成 ETag
    var etag = $"\"{article.UpdatedAt.Ticks}\"";

    Response.Headers.CacheControl = "public, max-age=60, must-revalidate";
    Response.Headers.ETag = etag;

    // 檢查條件請求
    if (Request.Headers.IfNoneMatch == etag)
    {
        return StatusCode(304); // Not Modified
    }

    return Ok(article);
}
```

### Program.cs 服務註冊

```csharp
// 註冊 HybridCache
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),           // L2 快取時間
        LocalCacheExpiration = TimeSpan.FromMinutes(1)  // L1 快取時間
    };
});

// 註冊 Redis 分散式快取
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Configuration"];
});

// 註冊快取提供者和資料倉儲
builder.Services.AddScoped<ICacheProvider, HybridCacheProvider>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
```

## 總結

這個多層快取架構展示了如何結合：
- **客戶端快取（HTTP Cache-Control + ETag）**
- **伺服器端記憶體快取（HybridCache L1）**
- **分散式快取（HybridCache L2 - Redis）**

透過這三層快取，我們實現了：
- ✅ 極致的效能（99.9% 的請求不需要查詢資料庫）
- ✅ 大幅降低網路流量（99% 節省）
- ✅ 確保資料一致性（ETag 驗證機制）
- ✅ 良好的可擴展性（分散式快取支援）
- ✅ 符合 RFC 9111 規範（標準化的 HTTP 快取）

## 延伸閱讀

- [RFC 9111: HTTP Caching](https://datatracker.ietf.org/doc/html/rfc9111)
- [RFC 8246: HTTP Immutable Responses](https://datatracker.ietf.org/doc/html/rfc8246)
- [.NET 9 HybridCache 文件](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
- [專案部落格文章](./blog-article.md)

這正是現代 Web 應用程式的最佳實踐！
