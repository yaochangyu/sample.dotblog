# 深入探討 HTTP Client-Side Cache：從 RFC 9111 到實戰驗證

## 前言

在開發 Web 應用程式時，我們經常聽到「快取」這個詞。大多數開發者會立即想到 Redis、Memory Cache 這類伺服器端快取機制，但其實還有一個經常被忽略卻極其重要的快取層級：**HTTP Client-Side Cache**。

想像一個情境：你的 API 回傳一張使用者頭像，這張圖片基本上不會改變。如果每次使用者重新整理頁面，瀏覽器都要重新向伺服器請求這張圖片，那麼：
1. 浪費使用者的網路流量
2. 增加伺服器的負載
3. 降低頁面載入速度

如果我們能夠善用 HTTP Cache 機制，告訴瀏覽器「這個資源可以快取 1 小時」，那麼在這 1 小時內，瀏覽器完全不需要發送請求到伺服器，直接從本地快取讀取，速度近乎瞬間。

今天，我們就來深入探討 HTTP Client-Side Cache 的運作原理，並透過實驗來觀察各種 Cache-Control 指令的實際行為。

## HTTP Caching 的標準規範：RFC 9111

HTTP Caching 的行為是由 **[RFC 9111](https://datatracker.ietf.org/doc/html/rfc9111)** 規範所定義的（它取代了舊的 RFC 7234）。這份規範定義了快取的運作方式、相關的 HTTP 標頭，以及快取必須遵守的規則。

### Cache-Control 指令總覽

RFC 9111 定義了兩類 Cache-Control 指令：

#### Response Directives（回應指令）- 伺服器告訴快取如何處理回應

- **max-age=N** - 回應在 N 秒內被視為新鮮（fresh）
- **no-cache** - 快取必須在使用前向伺服器驗證
- **no-store** - 完全禁止快取儲存請求或回應
- **private** - 只允許私有快取（瀏覽器）快取，禁止共享快取（CDN）
- **public** - 明確允許任何快取儲存回應
- **must-revalidate** - 過期後必須重新驗證，不能使用過期的快取
- **s-maxage=N** - 覆蓋 max-age，但只針對共享快取
- **immutable** - 內容在 max-age 期間內永遠不會改變（RFC 8246 擴展）

#### Request Directives（請求指令）- 客戶端告訴快取如何處理請求

- **max-age=N** - 客戶端希望回應的年齡不超過 N 秒
- **no-cache** - 要求在使用前向伺服器驗證
- **no-store** - 禁止快取儲存
- **only-if-cached** - 客戶端只接受已快取的回應

### 條件請求與驗證機制

除了 Cache-Control，RFC 9111 還定義了條件請求的機制：

- **ETag / If-None-Match** - 使用實體標籤（entity tag）進行精確比對
- **Last-Modified / If-Modified-Since** - 使用修改時間進行比對
- **Vary** - 指定哪些請求標頭會影響回應內容

## 實驗環境建置

為了深入理解這些指令的實際行為，我建立了一個實驗專案，實作了 16 個不同的測試端點，每個端點展示不同的 Cache-Control 行為。

### 專案架構

```
Lab.HttpCache/
├── src/Lab.HttpCache.Api/
│   ├── Controllers/
│   │   ├── CacheController.cs          # HybridCache 範例
│   │   └── ClientCacheController.cs    # HTTP Client Cache 實驗端點
│   └── Program.cs
└── docker-compose.yml                   # Redis 服務
```

### 核心實驗端點

讓我們看看 `ClientCacheController.cs` 的關鍵實作：

```csharp
[ApiController]
[Route("api/[controller]")]
public class ClientCacheController : ControllerBase
{
    private static int _requestCounter = 0;

    // 1. max-age - 標準快取
    [HttpGet("max-age")]
    public IActionResult GetMaxAge([FromQuery] int seconds = 60)
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = $"public, max-age={seconds}";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            value = $"此回應可快取 {seconds} 秒",
            cacheControl = $"public, max-age={seconds}"
        });
    }

    // 2. no-cache - 必須驗證
    [HttpGet("no-cache")]
    public IActionResult GetNoCache()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var etag = $"\"{requestId}\"";

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304); // Not Modified
        }

        return Ok(new { requestId, timestamp = DateTime.UtcNow, etag });
    }

    // 3. no-store - 完全禁止快取
    [HttpGet("no-store")]
    public IActionResult GetNoStore()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "no-store";

        return Ok(new { requestId, timestamp = DateTime.UtcNow });
    }
}
```

## 實驗觀察與結果

### 實驗一：max-age - 標準快取行為

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/max-age
```

**回應標頭：**
```http
HTTP/1.1 200 OK
Cache-Control: public, max-age=60
Content-Type: application/json
```

**回應內容：**
```json
{
  "requestId": 1,
  "timestamp": "2026-01-07T11:59:47.267Z",
  "value": "此回應可快取 60 秒",
  "cacheControl": "public, max-age=60"
}
```

**觀察結果：**
- `requestId` 為 1，表示這是第一次請求
- 設定 `Cache-Control: public, max-age=60` 表示這個回應可以被任何快取（包括瀏覽器和 CDN）儲存 60 秒
- 在這 60 秒內，瀏覽器會直接使用快取，**完全不會發送請求到伺服器**

**應用場景：**
- 靜態資源（CSS、JS、圖片）
- 不常變動的 API 回應（如產品列表、分類選單）

---

### 實驗二：no-store - 完全禁止快取

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/no-store
```

**回應標頭：**
```http
HTTP/1.1 200 OK
Cache-Control: no-store
Content-Type: application/json
```

**回應內容：**
```json
{
  "requestId": 2,
  "timestamp": "2026-01-07T11:59:48.680Z",
  "cacheControl": "no-store"
}
```

**觀察結果：**
- `requestId` 為 2，表示這是第二次請求
- 設定 `Cache-Control: no-store` 表示快取**不得儲存**此回應的任何部分
- 每次請求都會完整執行，伺服器都會收到請求

**應用場景：**
- 包含敏感資料的 API（如使用者個人資訊、金融資料）
- 即時性要求極高的資料（如股票價格、聊天訊息）

---

### 實驗三：ETag 強驗證與條件請求

**第一次請求：**
```bash
curl -i http://localhost:5178/api/clientcache/etag-strong
```

**回應：**
```http
HTTP/1.1 200 OK
Cache-Control: max-age=30
ETag: "v1.0"
Content-Type: application/json

{
  "requestId": 3,
  "timestamp": "2026-01-07T11:59:57.812Z",
  "etag": "\"v1.0\"",
  "contentVersion": "v1.0"
}
```

**第二次請求（使用 If-None-Match）：**
```bash
curl -i -H "If-None-Match: \"v1.0\"" http://localhost:5178/api/clientcache/etag-strong
```

**回應：**
```http
HTTP/1.1 304 Not Modified
Cache-Control: max-age=30
ETag: "v1.0"
```

**觀察結果：**
- 第一次請求回傳 200 OK，包含完整的回應內容
- 伺服器在回應中設定了 `ETag: "v1.0"`
- 第二次請求時，客戶端發送 `If-None-Match: "v1.0"` 標頭
- 伺服器比對 ETag，發現內容未改變，回傳 **304 Not Modified**，不包含回應本體
- 這節省了網路流量，因為不需要傳輸完整的回應內容

**ETag 的優勢：**
- **精確性**：強 ETag（無 `W/` 前綴）表示位元組級別的精確匹配
- **節省流量**：304 回應通常只有幾百位元組
- **減少伺服器負載**：伺服器只需比對 ETag，不需重新生成完整回應

**應用場景：**
- API 回應內容可能改變，但改變頻率不高
- 需要精確控制快取驗證的場景

---

### 實驗四：no-cache - 必須驗證但可快取

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/no-cache
```

**回應：**
```http
HTTP/1.1 200 OK
Cache-Control: no-cache
ETag: "5"
Content-Type: application/json

{
  "requestId": 5,
  "timestamp": "2026-01-07T12:00:00.636Z",
  "etag": "\"5\"",
  "cacheControl": "no-cache"
}
```

**觀察結果：**
- `Cache-Control: no-cache` 表示快取**可以儲存**回應，但每次使用前**必須向伺服器驗證**
- 與 `no-store` 的差異：`no-cache` 允許快取儲存，但要求驗證；`no-store` 完全禁止儲存
- 瀏覽器會發送帶有 `If-None-Match` 的請求進行驗證
- 如果內容未改變，伺服器回傳 304，節省流量

**常見誤解：**
- **錯誤認知**：`no-cache` 表示「不快取」
- **正確理解**：`no-cache` 表示「可以快取，但必須驗證」

**應用場景：**
- 需要確保資料是最新的，但願意使用條件請求來節省流量
- 內容可能更新，但更新後希望立即反映給使用者

---

### 實驗五：Vary 標頭 - 內容協商

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/vary
```

**回應：**
```http
HTTP/1.1 200 OK
Cache-Control: public, max-age=60
Vary: Accept-Encoding, User-Agent
Content-Type: application/json

{
  "requestId": 6,
  "timestamp": "2026-01-07T12:00:11.519Z",
  "acceptEncoding": "none",
  "userAgent": "curl/8.14.1",
  "vary": "Accept-Encoding, User-Agent"
}
```

**觀察結果：**
- `Vary: Accept-Encoding, User-Agent` 告訴快取：這個回應的內容會根據請求的 `Accept-Encoding` 和 `User-Agent` 標頭而不同
- 快取必須為每個不同的標頭組合儲存**不同的版本**
- 例如：gzip 壓縮的版本、未壓縮的版本、不同瀏覽器的版本

**實際影響：**
假設伺服器根據 `Accept-Encoding` 回傳不同的壓縮版本：
1. Chrome 發送 `Accept-Encoding: gzip`，快取儲存 gzip 版本
2. 舊版 IE 發送 `Accept-Encoding: deflate`，快取儲存 deflate 版本
3. 兩者的快取是**獨立的**，不會互相干擾

**應用場景：**
- 內容協商（Content Negotiation）：根據 `Accept-Language` 回傳不同語言
- 回應式 API：根據 `User-Agent` 回傳不同的資料格式
- 壓縮：根據 `Accept-Encoding` 回傳不同的壓縮版本

---

### 實驗六：immutable - 永不改變的內容

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/immutable
```

**回應：**
```http
HTTP/1.1 200 OK
Cache-Control: public, max-age=31536000, immutable
Content-Type: application/json

{
  "requestId": 7,
  "timestamp": "2026-01-07T12:00:13.015Z",
  "cacheControl": "public, max-age=31536000, immutable"
}
```

**觀察結果：**
- `max-age=31536000` 表示快取 1 年（365 天 × 24 小時 × 60 分鐘 × 60 秒）
- `immutable` 表示在這 1 年內，內容**永遠不會改變**
- 瀏覽器即使在使用者按下「重新整理」時，也**不會**發送條件請求到伺服器
- 這是最極致的快取策略，完全消除不必要的請求

**為什麼需要 immutable？**

在沒有 `immutable` 之前，即使設定了 `max-age=31536000`，當使用者按下 F5 重新整理頁面時，瀏覽器仍然會發送條件請求（帶 `If-None-Match`）到伺服器。這是因為瀏覽器需要確保內容是最新的。

有了 `immutable`，瀏覽器知道內容永遠不會改變，因此完全省略條件請求。

**應用場景：**
- 使用版本號或 hash 的靜態資源：`app.a3f2b9c.js`、`style.7e8f1d4.css`
- 一旦發布就不會改變的圖片、字型檔案
- CDN 上的不可變資源

**最佳實踐：**
```html
<!-- 正確：檔案名稱包含 hash，內容永不改變 -->
<script src="/js/app.a3f2b9c.js"></script>
<!-- Cache-Control: public, max-age=31536000, immutable -->

<!-- 錯誤：檔案名稱固定，但內容可能改變 -->
<script src="/js/app.js"></script>
<!-- 不應該使用 immutable -->
```

---

### 實驗七：stale-while-revalidate - 優化使用者體驗

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/stale-while-revalidate
```

**回應：**
```http
HTTP/1.1 200 OK
Cache-Control: max-age=10, stale-while-revalidate=60
Content-Type: application/json

{
  "requestId": 8,
  "timestamp": "2026-01-07T12:00:14.477Z",
  "cacheControl": "max-age=10, stale-while-revalidate=60"
}
```

**觀察結果：**
- `max-age=10` 表示回應在 10 秒內是新鮮的
- `stale-while-revalidate=60` 表示在過期後 60 秒內，可以使用「過期的」快取，同時在背景重新驗證

**時間軸分析：**

```
時間 0s：首次請求，伺服器回傳資料並設定 Cache-Control
│
├─ 0s ~ 10s：快取新鮮期
│   └─ 瀏覽器直接使用快取，不發送任何請求
│
├─ 10s ~ 70s：stale-while-revalidate 期間
│   ├─ 瀏覽器**立即**回傳過期的快取給使用者（保證速度）
│   └─ **同時**在背景發送請求到伺服器，更新快取（保證新鮮度）
│
└─ 70s 之後：快取完全過期
    └─ 瀏覽器必須等待伺服器回應，才能顯示內容
```

**優勢：**
- **使用者體驗**：即使快取過期，使用者仍然可以立即看到內容（雖然可能稍舊）
- **效能**：避免使用者等待伺服器回應
- **新鮮度**：背景更新確保下次請求會使用新的資料

**應用場景：**
- 社交媒體的動態牆（略微過時的內容可以接受）
- 新聞列表（即時性要求中等）
- 產品目錄（庫存可能有輕微延遲）

---

### 實驗八：驗證 requestCounter

**測試指令：**
```bash
curl -i http://localhost:5178/api/clientcache/stats
```

**回應：**
```json
{
  "totalRequests": 8,
  "serverStartTime": "2026-01-07T11:59:45.775Z",
  "uptime": "00:00:30.199",
  "description": "顯示伺服器啟動後收到的總請求數"
}
```

**觀察結果：**
- 我們總共測試了 8 個不同的端點
- 伺服器確實收到了 8 個請求
- 這證明了我們的實驗設計是正確的：每個端點都有獨立的 `requestId`

**為什麼需要 requestCounter？**

在測試快取時，`requestCounter` 是一個關鍵的驗證工具：
- 如果快取生效，伺服器**不會**收到請求，`requestCounter` 不會增加
- 如果收到條件請求並回傳 304，伺服器會收到請求，`requestCounter` 會增加
- 透過觀察 `requestId`，我們可以清楚知道有多少請求真正到達伺服器

---

### 實驗九：客戶端控制快取 - Request Cache-Control 指令

在前面的實驗中，我們看到的都是**伺服器端**透過 Response 的 `Cache-Control` 標頭控制快取。但其實**客戶端**也可以透過 Request 的 `Cache-Control` 標頭來控制快取行為。

#### RFC 9111 定義的 Request Directives

客戶端可以在請求中使用以下指令：

| 指令 | 作用 |
|------|------|
| `max-age=N` | 客戶端只接受年齡不超過 N 秒的快取 |
| `max-stale[=N]` | 客戶端願意接受過期的快取（可選擇性指定過期時間範圍） |
| `min-fresh=N` | 客戶端要求回應至少在 N 秒內保持新鮮 |
| `no-cache` | 強制快取必須向伺服器驗證後才能使用 |
| `no-store` | 禁止儲存快取 |
| `no-transform` | 禁止代理伺服器轉換內容 |
| `only-if-cached` | 只接受已快取的回應，如果沒有快取就回傳 504 |

#### 實驗 9-1：強制重新驗證（no-cache）

**測試指令：**
```bash
# 一般請求（會使用快取）
curl -i http://localhost:5178/api/clientcache/client-controlled

# 客戶端強制重新驗證
curl -i -H "Cache-Control: no-cache" http://localhost:5178/api/clientcache/client-controlled
```

**第一次請求（無 Cache-Control）：**
```http
GET /api/clientcache/client-controlled HTTP/1.1

HTTP/1.1 200 OK
Cache-Control: public, max-age=60
ETag: "638734982345678901"

{
  "requestId": 10,
  "clientCacheControl": "none",
  "clientWantsNoCache": false
}
```

**第二次請求（帶 Cache-Control: no-cache）：**
```http
GET /api/clientcache/client-controlled HTTP/1.1
Cache-Control: no-cache

HTTP/1.1 200 OK
Cache-Control: public, max-age=60
ETag: "638734982398765432"

{
  "requestId": 11,
  "clientCacheControl": "no-cache",
  "clientWantsNoCache": true
}
```

**觀察結果：**
- 當客戶端發送 `Cache-Control: no-cache`，**即使伺服器設定了 max-age=60**，瀏覽器也會：
  1. 繞過本地快取
  2. 發送完整請求到伺服器（或帶 If-None-Match 條件請求）
  3. 等待伺服器回應
- 這就是瀏覽器「強制重新整理」（Ctrl+F5）的原理

#### 實驗 9-2：只接受新鮮的快取（max-age）

**測試指令：**
```bash
# 客戶端只接受 30 秒內的快取
curl -i -H "Cache-Control: max-age=30" http://localhost:5178/api/clientcache/client-controlled
```

**行為：**
- 即使伺服器設定 `max-age=60`，瀏覽器也會：
  - 如果快取年齡 > 30 秒：重新向伺服器請求
  - 如果快取年齡 ≤ 30 秒：使用快取

**實際應用場景：**
```javascript
// JavaScript Fetch API 範例
fetch('/api/data', {
  headers: {
    'Cache-Control': 'max-age=30'
  }
})
```

這在 PWA（Progressive Web App）中很有用，可以確保資料的新鮮度。

#### 實驗 9-3：只使用快取（only-if-cached）

**測試指令：**
```bash
curl -i -H "Cache-Control: only-if-cached" http://localhost:5178/api/clientcache/client-controlled
```

**行為：**
- 如果有快取：回傳快取的內容
- 如果沒有快取：回傳 **504 Gateway Timeout**（不發送請求到伺服器）

**應用場景：**
- 離線模式的 PWA
- 避免在弱網路環境下等待過久
- 「只顯示已載入的內容」功能

#### 實驗 9-4：願意接受過期快取（max-stale）

**測試指令：**
```bash
# 願意接受任何過期的快取
curl -i -H "Cache-Control: max-stale" http://localhost:5178/api/clientcache/client-controlled

# 只接受過期不超過 300 秒的快取
curl -i -H "Cache-Control: max-stale=300" http://localhost:5178/api/clientcache/client-controlled
```

**行為：**
- `max-stale`：接受任何過期的快取
- `max-stale=300`：接受過期不超過 5 分鐘的快取

**應用場景：**
- 降低伺服器負載
- 網路不穩定時仍能提供服務
- 對資料即時性要求不高的場景

#### 瀏覽器的實際行為

讓我們用 Chrome 來觀察實際行為：

**1. 一般導航（點擊連結、輸入網址）：**
```
不發送 Cache-Control
→ 完全遵守伺服器的 Cache-Control 設定
→ 在 max-age 期間內使用快取
```

**2. 一般重新整理（F5）：**
```
發送: Cache-Control: max-age=0
（或不發送，但會帶 If-None-Match）
→ 允許使用快取，但必須驗證
→ 如果內容未變更，收到 304
```

**3. 強制重新整理（Ctrl+F5）：**
```
發送: Cache-Control: no-cache
       Pragma: no-cache
→ 完全繞過快取
→ 即使內容未變更，也回傳 200（不是 304）
```

#### 實務範例：自訂快取策略

在實際應用中，我們可以根據需求自訂快取行為：

```javascript
// React 範例：根據資料重要性調整快取策略
async function fetchData(url, freshness = 'normal') {
  const cacheHeaders = {
    'critical': { 'Cache-Control': 'no-cache' },        // 關鍵資料，必須驗證
    'normal': { },                                       // 一般資料，遵守伺服器設定
    'tolerant': { 'Cache-Control': 'max-stale=300' }    // 可容忍過期的資料
  };

  const response = await fetch(url, {
    headers: cacheHeaders[freshness]
  });

  return response.json();
}

// 使用範例
const criticalData = await fetchData('/api/balance', 'critical');      // 帳戶餘額，必須最新
const normalData = await fetchData('/api/products', 'normal');         // 產品列表，可以快取
const tolerantData = await fetchData('/api/news', 'tolerant');         // 新聞，可接受略舊
```

#### 服務端處理客戶端指令

伺服器可以讀取客戶端的 Cache-Control 並做出相應處理：

```csharp
[HttpGet("flexible")]
public IActionResult GetFlexible()
{
    var clientCacheControl = Request.Headers.CacheControl.FirstOrDefault();

    // 如果客戶端要求 no-cache，可以決定是否回傳更新的資料
    if (clientCacheControl?.Contains("no-cache") == true)
    {
        // 強制生成最新資料
        var freshData = GenerateFreshData();
        Response.Headers.CacheControl = "public, max-age=0, must-revalidate";
        return Ok(freshData);
    }

    // 一般情況，使用標準快取策略
    Response.Headers.CacheControl = "public, max-age=300";
    return Ok(GetCachedOrFreshData());
}
```

#### 客戶端控制的優先順序

**重要：** 客戶端的 Cache-Control 請求指令**不能強制伺服器改變回應的快取設定**，但可以：
1. ✅ 控制**自己的快取行為**（如何使用本地快取）
2. ✅ 影響**中間代理**（Proxy、CDN）的快取決策
3. ❌ 無法強制伺服器改變回應的 Cache-Control 標頭

**範例：**
```
客戶端請求: Cache-Control: max-age=10
伺服器回應: Cache-Control: public, max-age=60

結果:
- 瀏覽器只會快取 10 秒（取較小值）
- CDN 會快取 60 秒（使用伺服器設定）
```

#### 小結

客戶端控制快取是一個強大但經常被忽略的功能：

| 場景 | 客戶端指令 | 效果 |
|------|----------|------|
| PWA 離線模式 | `only-if-cached` | 只使用快取，不發送網路請求 |
| 確保資料新鮮 | `no-cache` 或 `max-age=0` | 強制驗證 |
| 弱網路環境 | `max-stale=300` | 接受略舊的資料 |
| 限制快取時間 | `max-age=30` | 覆蓋伺服器的較長快取時間 |
| 強制重新整理 | `no-cache` + `Pragma: no-cache` | 完全繞過快取 |

**關鍵理解：**
- 伺服器的 Cache-Control 定義了**允許的最大快取時間**
- 客戶端的 Cache-Control 可以**縮短但無法延長**這個時間
- 這是一個**協商機制**，而不是單方面的控制

---

## Cache-Control 指令總結與決策樹

### 快速決策指南

選擇合適的 Cache-Control 指令可能會讓人困惑，這裡提供一個決策流程：

```
是否包含敏感資料？
├─ 是 → 使用 no-store（完全禁止快取）
└─ 否 → 內容是否會改變？
    ├─ 永不改變（如 versioned 靜態檔案）
    │   └─ 使用 public, max-age=31536000, immutable
    │
    ├─ 很少改變（如產品圖片、CSS/JS）
    │   ├─ 公開內容 → public, max-age=86400（1天）
    │   └─ 使用者特定 → private, max-age=3600（1小時）
    │
    ├─ 中等頻率改變（如產品列表、文章列表）
    │   └─ public, max-age=300（5分鐘）, must-revalidate
    │       或 max-age=60, stale-while-revalidate=300
    │
    └─ 經常改變但接受輕微延遲
        └─ max-age=10, stale-while-revalidate=60
```

### 各指令比較表

| 指令 | 是否儲存快取 | 是否驗證 | 使用場景 |
|------|------------|----------|----------|
| `max-age=N` | ✅ | 過期後驗證 | 標準快取，N 秒內直接使用 |
| `no-cache` | ✅ | 每次都驗證 | 需要確保新鮮度，但願意使用 304 |
| `no-store` | ❌ | N/A | 敏感資料，完全禁止快取 |
| `private` | ✅ (僅瀏覽器) | 根據其他指令 | 使用者特定資料 |
| `public` | ✅ (包括 CDN) | 根據其他指令 | 公開資源 |
| `must-revalidate` | ✅ | 過期後必須驗證 | 不能使用過期的快取 |
| `immutable` | ✅ | 永不驗證 | 永不改變的 versioned 資源 |

### s-maxage 與 max-age 的差異

```
設定：Cache-Control: public, max-age=60, s-maxage=3600

┌─────────────┐     ┌─────────┐     ┌──────────┐
│   瀏覽器    │────→│   CDN   │────→│  伺服器  │
│ (max-age)   │     │(s-maxage)│     │          │
│  快取60秒   │     │ 快取3600秒│     │          │
└─────────────┘     └─────────┘     └──────────┘
```

**運作流程：**
1. 首次請求：瀏覽器 → CDN → 伺服器
2. CDN 快取回應 3600 秒（1小時）
3. 瀏覽器快取回應 60 秒（1分鐘）
4. 1 分鐘內：瀏覽器直接使用本地快取
5. 1 分鐘後 ~ 1 小時內：瀏覽器請求 CDN，CDN 回傳快取
6. 1 小時後：CDN 請求伺服器更新快取

**應用場景：**
- CDN 加速的公開 API
- 希望 CDN 快取時間長，但瀏覽器快取時間短的場景

## ResponseCache vs Response.Headers.CacheControl

在 ASP.NET Core 中，設定 HTTP Cache 有兩種主要方式：宣告式的 `[ResponseCache]` 屬性和命令式的 `Response.Headers.CacheControl`。理解它們的差異對於選擇正確的實作方式至關重要。

### 方式一：`[ResponseCache]` 屬性（宣告式）

`[ResponseCache]` 是一個 Action Filter Attribute，提供宣告式的快取設定方式。

**範例實作：**
```csharp
[HttpGet("http-response-cache")]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
public IActionResult GetHttpResponseCache()
{
    return Ok(new
    {
        source = "HTTP Response Cache (60 seconds)",
        value = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
        timestamp = DateTime.UtcNow,
        description = "使用 ResponseCache Attribute 設定客戶端快取"
    });
}
```

**實際產生的 HTTP 標頭：**
```http
HTTP/1.1 200 OK
Cache-Control: public, max-age=60
Content-Type: application/json
```

**特點：**
- ✅ **宣告式語法** - 清楚表達快取意圖，程式碼可讀性高
- ✅ **型別安全** - 編譯時期檢查參數有效性
- ✅ **自動處理** - 框架自動將設定轉換為正確的 HTTP 標頭
- ✅ **支援 CacheProfile** - 可在 `Program.cs` 定義共享設定

**CacheProfile 範例：**
```csharp
// Program.cs
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("Default30",
        new CacheProfile
        {
            Duration = 30,
            Location = ResponseCacheLocation.Any
        });
    
    options.CacheProfiles.Add("NeverCache",
        new CacheProfile
        {
            Location = ResponseCacheLocation.None,
            NoStore = true
        });
});

// Controller
[ResponseCache(CacheProfileName = "Default30")]
public IActionResult GetProduct(int id) { ... }
```

**參數說明：**
- `Duration` - 快取持續時間（秒），對應 `max-age`
- `Location` - 快取位置
  - `Any` → `public`（任何快取都可儲存）
  - `Client` → `private`（僅瀏覽器快取）
  - `None` → `no-cache`（必須驗證）
- `NoStore` - 設為 `true` 時產生 `no-store`
- `VaryByHeader` - 設定 `Vary` 標頭
- `VaryByQueryKeys` - 根據查詢參數變化快取

### 方式二：`Response.Headers.CacheControl`（命令式）

直接操作 HTTP 標頭，提供更細粒度的控制。

**範例實作：**
```csharp
[HttpGet("http-cache-control")]
public IActionResult GetHttpCacheControl()
{
    Response.Headers.CacheControl = "public, max-age=120";
    Response.Headers.Expires = DateTime.UtcNow.AddMinutes(2).ToString("R");

    return Ok(new
    {
        source = "HTTP Cache-Control (120 seconds)",
        value = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
        timestamp = DateTime.UtcNow,
        headers = new
        {
            CacheControl = "public, max-age=120",
            Expires = Response.Headers.Expires.ToString()
        },
        description = "手動設定 Cache-Control 和 Expires 標頭"
    });
}
```

**實際產生的 HTTP 標頭：**
```http
HTTP/1.1 200 OK
Cache-Control: public, max-age=120
Expires: Wed, 08 Jan 2026 04:38:00 GMT
Content-Type: application/json
```

**特點：**
- ✅ **動態控制** - 執行時期根據條件決定快取策略
- ✅ **完整控制** - 可設定任意標頭組合（Expires、Vary、ETag 等）
- ✅ **彈性高** - 適合複雜的條件邏輯
- ✅ **精確設定** - 可使用 RFC 9111 的所有指令

**動態快取範例：**
```csharp
[HttpGet("dynamic-cache")]
public IActionResult GetDynamicCache([FromQuery] bool isVip)
{
    if (isVip)
    {
        // VIP 使用者：快取 5 分鐘
        Response.Headers.CacheControl = "private, max-age=300";
    }
    else
    {
        // 一般使用者：快取 1 分鐘
        Response.Headers.CacheControl = "private, max-age=60";
    }

    return Ok(GetUserData());
}
```

**ETag 條件請求範例：**
```csharp
[HttpGet("data/{id}")]
public IActionResult GetData(int id)
{
    var data = _repository.Get(id);
    var etag = $"\"{data.Version}\"";

    // 設定 ETag 和 Cache-Control
    Response.Headers.ETag = etag;
    Response.Headers.CacheControl = "public, max-age=60";

    // 處理條件請求
    if (Request.Headers.IfNoneMatch == etag)
    {
        return StatusCode(304); // Not Modified
    }

    return Ok(data);
}
```

### 比較總結

| 特性 | `[ResponseCache]` | `Response.Headers.CacheControl` |
|------|------------------|--------------------------------|
| **設定方式** | 宣告式（Attribute） | 命令式（程式碼） |
| **可讀性** | ⭐⭐⭐⭐⭐ 清晰易懂 | ⭐⭐⭐ 需要理解 HTTP 標頭 |
| **動態決策** | ❌ 編譯時期固定 | ✅ 執行時期決定 |
| **條件邏輯** | ❌ 不支援 | ✅ 完全支援 |
| **型別安全** | ✅ 編譯時期檢查 | ⚠️ 字串值，執行時期檢查 |
| **共享設定** | ✅ CacheProfile | ❌ 需手動實作 |
| **複雜標頭** | ⚠️ 有限支援 | ✅ 完全控制 |
| **ETag 處理** | ❌ 需額外實作 | ✅ 完整控制 |
| **適用場景** | 靜態、固定的快取策略 | 動態、條件式的快取邏輯 |

### 選擇指南

**使用 `[ResponseCache]` 當：**
1. 快取策略是固定的（如靜態資源）
2. 想要集中管理快取設定（CacheProfile）
3. 優先考慮程式碼可讀性和維護性
4. 快取行為不需要根據請求條件改變

```csharp
// ✅ 適合：產品列表（固定快取 5 分鐘）
[HttpGet("products")]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
public IActionResult GetProducts() { ... }
```

**使用 `Response.Headers.CacheControl` 當：**
1. 需要根據條件動態決定快取策略
2. 需要設定 ETag、Last-Modified 等驗證標頭
3. 需要組合多個標頭（Vary、Expires）
4. 實作複雜的快取邏輯（如根據使用者角色、資料狀態）

```csharp
// ✅ 適合：根據資料新鮮度動態決定
[HttpGet("news/{id}")]
public IActionResult GetNews(int id)
{
    var news = _repository.Get(id);
    var age = DateTime.UtcNow - news.PublishedAt;

    if (age.TotalHours < 1)
    {
        // 新聞剛發布：快取 1 分鐘
        Response.Headers.CacheControl = "public, max-age=60";
    }
    else if (age.TotalDays < 7)
    {
        // 一週內：快取 30 分鐘
        Response.Headers.CacheControl = "public, max-age=1800";
    }
    else
    {
        // 舊聞：快取 24 小時
        Response.Headers.CacheControl = "public, max-age=86400";
    }

    return Ok(news);
}
```

### 混合使用範例

在實務中，也可以混合使用兩種方式：

```csharp
[ResponseCache(Duration = 60, VaryByHeader = "Accept-Language")]
public IActionResult GetLocalized()
{
    // 基礎設定由 Attribute 處理
    // 額外的動態邏輯由程式碼處理
    
    if (IsDataStale())
    {
        // 資料已過時，覆蓋 Attribute 的設定
        Response.Headers.CacheControl = "no-cache";
    }

    return Ok(GetLocalizedData());
}
```

**注意：** 當同時使用時，`Response.Headers.CacheControl` 的設定會覆蓋 `[ResponseCache]` 的設定。

## 實務建議

### 1. 靜態資源的最佳實踐

**使用版本化檔名 + immutable：**
```http
# 好的做法
GET /assets/app.a3f2b9c.js
Cache-Control: public, max-age=31536000, immutable

# 壞的做法
GET /assets/app.js
Cache-Control: public, max-age=86400
```

**為什麼？**
- 版本化檔名確保內容永不改變，可以安全使用長期快取
- 當程式碼更新時，產生新的檔名（如 `app.b4e5f8d.js`），舊的快取自動失效
- `immutable` 完全消除不必要的驗證請求

### 2. API 回應的快取策略

**區分資料的變動頻率：**

```csharp
// 使用者個人資料（很少改變）
[HttpGet("profile")]
public IActionResult GetProfile()
{
    Response.Headers.CacheControl = "private, max-age=3600";
    Response.Headers.ETag = GenerateETag(userData);
    // ...
}

// 產品列表（中等頻率改變）
[HttpGet("products")]
public IActionResult GetProducts()
{
    Response.Headers.CacheControl = "public, max-age=300, must-revalidate";
    Response.Headers.ETag = GenerateETag(products);
    // ...
}

// 即時股票價格（高頻率改變）
[HttpGet("stock-price")]
public IActionResult GetStockPrice()
{
    Response.Headers.CacheControl = "no-store";
    // ...
}
```

### 3. 結合 ETag 提升效率

**完整的快取 + 驗證策略：**

```csharp
[HttpGet("article/{id}")]
public IActionResult GetArticle(int id)
{
    var article = _repository.GetArticle(id);
    var etag = $"\"{article.UpdatedAt.Ticks}\"";

    Response.Headers.CacheControl = "public, max-age=60, must-revalidate";
    Response.Headers.ETag = etag;

    // 檢查條件請求
    if (Request.Headers.IfNoneMatch == etag)
    {
        return StatusCode(304); // Not Modified - 節省流量
    }

    return Ok(article);
}
```

**流程：**
1. 首次請求：伺服器回傳完整資料 + ETag
2. 60 秒內：瀏覽器直接使用快取（0 個請求到伺服器）
3. 60 秒後：瀏覽器發送條件請求（帶 If-None-Match）
4. 如果內容未改變：伺服器回傳 304（幾百位元組）
5. 如果內容改變：伺服器回傳 200 + 新資料 + 新 ETag

### 4. 使用 Vary 標頭處理內容協商

**根據不同標頭回傳不同內容：**

```csharp
[HttpGet("data")]
public IActionResult GetData()
{
    var acceptLanguage = Request.Headers.AcceptLanguage.FirstOrDefault() ?? "en";

    Response.Headers.CacheControl = "public, max-age=600";
    Response.Headers.Vary = "Accept-Language";

    // 根據語言回傳不同資料
    var data = GetLocalizedData(acceptLanguage);
    return Ok(data);
}
```

**注意事項：**
- 每個不同的 `Accept-Language` 值都會產生獨立的快取
- 過多的 Vary 標頭會降低快取命中率
- 謹慎選擇 Vary 的標頭

### 5. 開發環境 vs 生產環境

**開發時禁用快取：**
```csharp
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        await next();
    });
}
```

**生產環境啟用快取：**
```csharp
if (app.Environment.IsProduction())
{
    // 使用 ResponseCaching 中介軟體
    app.UseResponseCaching();
}
```

## 常見陷阱與誤區

### 陷阱 1：no-cache ≠ 不快取

**錯誤認知：**
```
no-cache = 完全不快取
```

**正確理解：**
```
no-cache = 可以快取，但每次使用前必須驗證
no-store = 完全不快取
```

### 陷阱 2：max-age=0 與 no-cache 的細微差異

雖然兩者效果類似（都要求重新驗證），但語意上有差異：
- `max-age=0`：快取立即過期，需要重新驗證
- `no-cache`：可以快取，但不能在未驗證的情況下使用

實務上，兩者的行為幾乎相同。

### 陷阱 3：忘記處理條件請求

**不完整的實作：**
```csharp
// 只設定 ETag，但沒有處理 If-None-Match
[HttpGet("data")]
public IActionResult GetData()
{
    Response.Headers.ETag = "\"v1.0\"";
    return Ok(data); // 每次都回傳完整資料！
}
```

**完整的實作：**
```csharp
[HttpGet("data")]
public IActionResult GetData()
{
    var etag = "\"v1.0\"";
    Response.Headers.ETag = etag;

    // 處理條件請求
    if (Request.Headers.IfNoneMatch == etag)
    {
        return StatusCode(304); // 節省流量
    }

    return Ok(data);
}
```

### 陷阱 4：過度快取動態內容

**危險的設定：**
```csharp
// 使用者的購物車內容，但快取 1 小時？
[HttpGet("cart")]
public IActionResult GetCart()
{
    Response.Headers.CacheControl = "public, max-age=3600"; // 錯誤！
    return Ok(userCart);
}
```

**問題：**
- 使用者 A 的購物車可能被快取並回傳給使用者 B（如果使用 CDN）
- 使用者加入新商品後，看不到更新

**正確做法：**
```csharp
[HttpGet("cart")]
public IActionResult GetCart()
{
    Response.Headers.CacheControl = "private, no-store"; // 完全不快取
    // 或
    Response.Headers.CacheControl = "private, max-age=0, must-revalidate";
    return Ok(userCart);
}
```

## 效能影響分析

### 快取命中率的威力

假設一個 API 端點：
- 回應大小：50 KB
- 每秒請求數：100 req/s
- 伺服器處理時間：20ms

**沒有快取：**
- 每秒流量：50 KB × 100 = 5 MB/s
- 伺服器負載：100 req/s × 20ms = 2000ms = 2 CPU 秒/秒

**使用 max-age=60（60秒快取）：**
- 瀏覽器在 60 秒內完全不發送請求
- 實際到達伺服器的請求數：~0 req/s（同一使用者的重複請求）
- **流量節省：接近 100%**
- **伺服器負載：接近 0%**

**使用 no-cache + ETag：**
- 瀏覽器每次都發送條件請求
- 如果內容未改變（假設 95% 機率），回傳 304
- 304 回應大小：~200 bytes
- 每秒流量：200 bytes × 100 = 20 KB/s
- **流量節省：~99.6%**（從 5 MB/s 降到 20 KB/s）
- 伺服器仍需處理請求，但不需重新生成回應

### 真實世界的數據

根據 Google 的研究：
- 正確使用 HTTP Cache 可以減少 **60-90%** 的網路流量
- 靜態資源使用 `immutable` 可以完全消除驗證請求，節省 **數千個不必要的請求**
- `stale-while-revalidate` 可以將感知延遲降低 **50-80%**

## 總結

HTTP Client-Side Cache 是一個強大但經常被忽視的效能優化工具。透過正確使用 Cache-Control 指令，我們可以：

1. **大幅降低伺服器負載** - 快取命中的請求完全不會到達伺服器
2. **節省網路流量** - 使用條件請求和 304 回應可以節省 99% 的流量
3. **提升使用者體驗** - 快取回應的載入速度接近瞬間
4. **降低成本** - 減少伺服器資源使用和 CDN 流量成本

**關鍵要點：**
- **max-age** 是最基本也最有效的快取策略
- **ETag** 提供精確的驗證機制，適合內容可能改變的場景
- **immutable** 是靜態資源的終極優化
- **no-cache** 不等於不快取，它表示「必須驗證」
- **Vary** 確保內容協商的正確性
- **stale-while-revalidate** 在效能和新鮮度之間取得平衡

記住，快取是一把雙面刃：正確使用能帶來巨大的效能提升，錯誤使用可能導致使用者看到過時的資料。始終根據資料的特性（敏感性、變動頻率、即時性要求）來選擇合適的快取策略。

## 參考資源

- [RFC 9111: HTTP Caching](https://datatracker.ietf.org/doc/html/rfc9111)
- [RFC 8246: HTTP Immutable Responses](https://datatracker.ietf.org/doc/html/rfc8246)
- [RFC 5861: HTTP Cache-Control Extensions for Stale Content](https://datatracker.ietf.org/doc/html/rfc5861)
- [Cache-Control header - MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cache-Control)

## 實驗專案程式碼

完整的實驗程式碼位於專案的 `src/Lab.HttpCache.Api/Controllers/ClientCacheController.cs`，包含：
- 16 個不同的測試端點
- 詳細的註解說明
- Docker Compose 配置
- 測試指令範例

歡迎自行實驗，觀察不同 Cache-Control 指令的實際行為！

---

**實驗心得**

透過這次實驗，我深刻體會到 HTTP Cache 的威力。在實作過程中，我特別注意到幾個有趣的現象：

1. **requestCounter 的價值** - 透過 requestId，可以清楚看到哪些請求真正到達伺服器，哪些被快取攔截
2. **304 的高效** - 相比完整的 200 回應，304 只需傳輸標頭，節省的流量非常可觀
3. **immutable 的革命性** - 完全消除驗證請求，這對於大型網站的靜態資源來說是巨大的優化

最重要的是，HTTP Cache 是**標準化**的，不需要額外的程式庫或框架，只需要正確設定 HTTP 標頭，就能獲得瀏覽器和 CDN 的原生支援。

希望這篇文章能幫助你更好地理解和運用 HTTP Client-Side Cache！

若有謬誤，煩請告知，謝謝。
