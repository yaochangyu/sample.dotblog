# HTTP Client-Side Cache 實戰指南

## 前言

HTTP Client-Side Cache 是經常被忽略的效能優化工具。透過正確的 HTTP 標頭設定,可以讓瀏覽器直接使用本地快取,減少伺服器負載並提升使用者體驗。

本文透過實際程式碼示範各種 Cache-Control 指令的行為與應用場景。

## 核心概念

### 常用 Cache-Control 指令

- **max-age=N** - 快取 N 秒
- **no-cache** - 可快取但必須驗證
- **no-store** - 完全禁止快取
- **private** - 僅瀏覽器可快取
- **immutable** - 內容永不改變

### 驗證機制

- **ETag / If-None-Match** - 實體標籤比對
- **Last-Modified / If-Modified-Since** - 時間比對

## 快速啟動

```bash
# 1. 啟動 Redis
docker-compose up -d

# 2. 啟動 API
cd src\Lab.HttpCache.Api
dotnet run
```

## 實驗一：max-age

```csharp
[HttpGet("max-age")]
public IActionResult GetMaxAge([FromQuery] int seconds = 60)
{
    Response.Headers.CacheControl = $"public, max-age={seconds}";
    return Ok(new { requestId = Interlocked.Increment(ref _requestCounter) });
}
```

**測試：**

```http
@baseUrl = http://localhost:5178

### 首次請求
GET {{baseUrl}}/api/clientcache/max-age?seconds=60
Accept: application/json

### 立即再次請求（60秒內）
GET {{baseUrl}}/api/clientcache/max-age?seconds=60
Accept: application/json
```

在瀏覽器 DevTools 中,60 秒內重新整理會看到 `(from cache)`,請求不會到達伺服器。

**應用：** 靜態資源、不常變動的 API（導覽選單、分類）

## 實驗二：no-store

```csharp
[HttpGet("no-store")]
public IActionResult GetNoStore()
{
    Response.Headers.CacheControl = "no-store";
    return Ok(new { requestId = Interlocked.Increment(ref _requestCounter) });
}
```

**測試：**

```http
### 首次請求
GET {{baseUrl}}/api/clientcache/no-store
Accept: application/json

### 第二次請求
GET {{baseUrl}}/api/clientcache/no-store
Accept: application/json

### 第三次請求
GET {{baseUrl}}/api/clientcache/no-store
Accept: application/json
```

每次請求 `requestId` 都會遞增,表示完全不使用快取。

**應用：** 敏感資料（個人資訊、交易記錄）

## 實驗三：ETag 驗證

```csharp
[HttpGet("article/{id}")]
public async Task<IActionResult> GetArticle(int id)
{
    var article = await _articleRepository.GetByIdAsync(id);
    var etag = $"\"{article.Version}\"";
    
    if (Request.Headers.IfNoneMatch == etag)
        return StatusCode(304); // Not Modified
    
    Response.Headers.ETag = etag;
    return Ok(article);
}
```

**測試：**

```http
### 首次請求
GET {{baseUrl}}/api/clientcache/article/1
Accept: application/json

### 第二次請求（會自動帶 If-None-Match）
GET {{baseUrl}}/api/clientcache/article/1
Accept: application/json
```

第二次請求帶上 `If-None-Match: "版本號"`,若未變更會收到 304（無 Body,節省 99% 流量）。

**應用：** 經常查詢但不常變更的資料（文章、產品詳情）

## 實驗四：no-cache vs no-store

| 指令 | 是否快取 | 是否驗證 | 應用 |
|------|---------|---------|------|
| no-cache | ✅ 快取 | ✅ 必須驗證 | HTML 頁面 |
| no-store | ❌ 不快取 | ❌ 不驗證 | 敏感資料 |

## 實驗五：immutable

```csharp
[HttpGet("immutable")]
public IActionResult GetImmutable()
{
    Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    return Ok(new { requestId = Interlocked.Increment(ref _requestCounter) });
}
```

**測試：**

```http
### 請求 immutable 資源
GET {{baseUrl}}/api/clientcache/immutable
Accept: application/json
```

**特性：** 即使 Ctrl+F5 強制重新整理,瀏覽器也不會發送請求。

**應用：** 版本化靜態資源（`app.a1b2c3.js`、CDN 資源）

## 實驗六：stale-while-revalidate

```csharp
[HttpGet("stale-while-revalidate")]
public IActionResult GetStaleWhileRevalidate()
{
    Response.Headers.CacheControl = "max-age=10, stale-while-revalidate=60";
    return Ok(new { requestId = Interlocked.Increment(ref _requestCounter) });
}
```

**測試：**

```http
### 首次請求
GET {{baseUrl}}/api/clientcache/stale-while-revalidate
Accept: application/json

### 15 秒後請求（會先回傳舊快取）
GET {{baseUrl}}/api/clientcache/stale-while-revalidate
Accept: application/json
```

**行為：** 過期後立即回傳舊快取,背景重新驗證。使用者永遠獲得即時回應。

**應用：** 新聞列表、產品列表、社交動態

## 實驗七：Vary

```csharp
[HttpGet("vary")]
public IActionResult GetVary()
{
    Response.Headers.CacheControl = "public, max-age=300";
    Response.Headers.Vary = "Accept-Encoding";
    return Ok(new { encoding = Request.Headers.AcceptEncoding.ToString() });
}
```

**測試：**

```http
### 使用 gzip 編碼
GET {{baseUrl}}/api/clientcache/vary
Accept-Encoding: gzip

### 使用 br 編碼
GET {{baseUrl}}/api/clientcache/vary
Accept-Encoding: br
```

**作用：** 告訴快取根據指定標頭分別儲存（如 gzip 和 br 是不同的快取項目）。

**應用：** 內容壓縮、多語言、API 版本控制

## 實驗八：組合策略

同時使用 `Cache-Control + ETag + Last-Modified` 可提供最佳相容性。驗證優先順序：
1. 檢查 `If-None-Match` (ETag)
2. 檢查 `If-Modified-Since` (時間)

ETag 更精確（內容雜湊）,Last-Modified 僅精確到秒。

## 最佳實踐

| 資源類型 | 策略 | 範例 |
|---------|------|------|
| 版本化靜態資源 | `max-age=31536000, immutable` | `app.a1b2c3.js` |
| 不常變動 API | `max-age=300` | 分類、選單 |
| 經常查詢 API | `max-age=0, must-revalidate` + ETag | 文章、產品 |
| 即時性 API | `private, no-cache` + ETag | 庫存、價格 |
| 敏感資料 | `private, no-store` | 個資、交易 |

## 常見誤區

1. **no-cache ≠ 不快取**：`no-cache` 會快取但必須驗證,`no-store` 才是完全不快取
2. **只設 ETag 不處理 If-None-Match**：每次仍傳輸完整資料
3. **immutable 用於會變動的資源**：只適用永不改變的版本化資源

## 總結

HTTP Client-Side Cache 透過正確設定 HTTP 標頭即可獲得瀏覽器原生支援：

- **max-age** - 降低伺服器負載
- **ETag** - 節省 99% 流量
- **immutable** - 消除驗證請求
- **stale-while-revalidate** - 平衡效能與新鮮度

根據資料特性（敏感性、變動頻率）選擇合適策略,正確使用能大幅提升效能。

## 參考資源

- [RFC 9111: HTTP Caching](https://datatracker.ietf.org/doc/html/rfc9111)
- [RFC 8246: HTTP Immutable Responses](https://datatracker.ietf.org/doc/html/rfc8246)
- [MDN: Cache-Control](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control)
