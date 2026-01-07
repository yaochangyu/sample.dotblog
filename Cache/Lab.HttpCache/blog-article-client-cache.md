# [HTTP] Client Side Cache 實驗筆記：從 RFC 7234 規範到實際行為觀察

## 前言

最近在優化前端效能時，發現團隊對 HTTP Client Cache 的理解有些模糊。大家雖然都知道要加 `Cache-Control`，但對於 `max-age`、`no-cache`、`no-store` 這些指令的實際行為卻說不太清楚。更不用說 ETag 和 Last-Modified 的差異了。

後來我花了一些時間研究 RFC 7234（HTTP Caching）和 RFC 7232（Conditional Requests）這兩份規範，並且做了一系列實驗來驗證瀏覽器的實際行為。結果發現有些行為和我原本的理解不太一樣，特別記錄下來分享給大家。

## 開發環境

- .NET 9.0 SDK
- ASP.NET Core Web API
- Chrome 131 (開發者工具)
- Postman 11.x
- Fiddler Classic (用於觀察 HTTP 封包)

## RFC 7234 規範重點整理

在開始實驗前，先整理幾個重要的規範定義。

### Cache-Control 指令（RFC 7234）

根據 RFC 7234，Cache-Control 是用來控制快取行為的主要標頭：

**回應端常用指令：**
- `max-age=<seconds>`：回應被視為新鮮的最大秒數
- `no-cache`：**必須向伺服器驗證後才能使用快取**
- `no-store`：**禁止快取任何內容**（請求和回應都不能快取）
- `public`：允許快取（即使是需要驗證的請求）
- `private`：僅允許私有快取（瀏覽器），不允許共享快取（Proxy）
- `must-revalidate`：快取過期後必須向伺服器驗證

**重點：** RFC 7234 明確指出 `no-cache` 不是「不快取」，而是「必須驗證」。這個命名很容易讓人誤解。

### 條件請求（RFC 7232）

- `ETag`：實體標籤，用於識別資源的特定版本
- `If-None-Match`：與 ETag 配合，如果資源未變更會收到 304
- `Last-Modified`：資源最後修改時間
- `If-Modified-Since`：與 Last-Modified 配合，如果資源未變更會收到 304

**優先順序：** RFC 7232 規定若同時存在 ETag 和 Last-Modified，應優先使用 ETag 進行驗證。

### Freshness 計算

RFC 7234 定義的新鮮度計算公式：

```
response_is_fresh = (freshness_lifetime > current_age)
```

新鮮度生命週期（優先順序）：
1. `s-maxage` 指令（共享快取）
2. `max-age` 指令
3. `Expires` 標頭減去 `Date` 標頭
4. 啟發式過期（如果適用）

## 實驗環境準備

### 啟動測試 API

首先，我在 ASP.NET Core 中準備了測試專案。專案在 GitHub 上可以取得：

```bash
# Clone 專案
git clone https://github.com/yaochangyu/sample.dotblog.git
cd sample.dotblog/Cache/Lab.HttpCache

# 啟動 Redis（如果需要測試伺服器端快取）
docker-compose up -d

# 啟動 API
cd src/Lab.HttpCache.Api
dotnet run
```

API 會在 `http://localhost:5178` 和 `https://localhost:7288` 啟動。

以下是我準備的測試端點：

```csharp
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    /// <summary>
    /// 實驗 1：Cache-Control: max-age
    /// </summary>
    [HttpGet("max-age")]
    public IActionResult GetMaxAge()
    {
        Response.Headers.CacheControl = "public, max-age=60";

        return Ok(new
        {
            value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
            timestamp = DateTime.UtcNow,
            cacheControl = "public, max-age=60"
        });
    }

    /// <summary>
    /// 實驗 2：ETag 條件請求
    /// </summary>
    [HttpGet("etag")]
    public IActionResult GetETag()
    {
        var data = $"Data at {DateTime.UtcNow:HH:mm:ss}";
        var etag = $"\"{data.GetHashCode()}\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=60";

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304); // Not Modified
        }

        return Ok(new
        {
            value = data,
            etag,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 實驗 3：no-cache vs no-store
    /// </summary>
    [HttpGet("no-cache")]
    public IActionResult GetNoCache()
    {
        Response.Headers.CacheControl = "no-cache";

        return Ok(new
        {
            value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
            timestamp = DateTime.UtcNow,
            cacheControl = "no-cache"
        });
    }

    [HttpGet("no-store")]
    public IActionResult GetNoStore()
    {
        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
            timestamp = DateTime.UtcNow,
            cacheControl = "no-store"
        });
    }

    /// <summary>
    /// 實驗 4：private vs public
    /// </summary>
    [HttpGet("private")]
    public IActionResult GetPrivate()
    {
        Response.Headers.CacheControl = "private, max-age=60";

        return Ok(new
        {
            value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
            timestamp = DateTime.UtcNow,
            cacheControl = "private, max-age=60"
        });
    }

    /// <summary>
    /// 實驗 5：Expires vs Cache-Control
    /// </summary>
    [HttpGet("expires")]
    public IActionResult GetExpires()
    {
        Response.Headers.Expires = DateTime.UtcNow.AddMinutes(2).ToString("R");

        return Ok(new
        {
            value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
            timestamp = DateTime.UtcNow,
            expires = Response.Headers.Expires.ToString()
        });
    }
}
```

## 如何進行實驗：工具使用指南

在開始實驗前，我先說明如何使用各種工具來觀察 HTTP Cache 的行為。這些工具和方法在後續所有實驗中都會用到。

### 方法 1：使用 Chrome 瀏覽器（最推薦）

這是最直覺的方式，也是我主要使用的方法。

#### 步驟 1：開啟 Chrome DevTools

1. 開啟 Chrome 瀏覽器
2. 按 `F12` 或 `Ctrl+Shift+I` 開啟開發者工具
3. 切換到 **Network** 面板

#### 步驟 2：設定 Network 面板

為了清楚觀察快取行為，建議調整以下設定：

1. **顯示完整欄位**：在欄位標題上按右鍵，勾選以下欄位
   - `Status`：顯示 HTTP 狀態碼
   - `Type`：顯示內容類型
   - `Size`：顯示檔案大小（重要！）
   - `Time`：顯示請求時間
   - `Cache-Control`：顯示快取標頭
   - `ETag`：顯示 ETag 值

2. **不要勾選 "Disable cache"**：這個選項會完全停用快取，實驗時要保持取消勾選

![Chrome DevTools Network 面板設定](https://i.imgur.com/example1.png)

#### 步驟 3：觀察快取行為

發送請求後，觀察 **Size** 欄位：

- `150 B`：實際從伺服器下載的大小
- `(disk cache)`：從磁碟快取讀取（瀏覽器關閉後仍保留）
- `(memory cache)`：從記憶體快取讀取（最快，但關閉分頁就消失）

觀察 **Time** 欄位：

- `25 ms`：實際網路往返時間
- `0 ms` 或 `1-5 ms`：從快取讀取（幾乎即時）

觀察 **Status** 欄位：

- `200 OK`：正常回應
- `200 OK (from disk cache)`：從磁碟快取讀取
- `200 OK (from memory cache)`：從記憶體快取讀取
- `304 Not Modified`：條件請求，內容未變更

#### 步驟 4：測試不同的重新整理方式

Chrome 有三種重新整理方式，行為完全不同：

| 操作 | 快捷鍵 | 行為 | 發送標頭 |
|------|-------|------|---------|
| **一般重新整理** | `F5` 或 `Ctrl+R` | 驗證快取 | `If-None-Match` / `If-Modified-Since` |
| **強制重新整理** | `Ctrl+F5` 或 `Ctrl+Shift+R` | 繞過快取 | `Cache-Control: no-cache`<br>`Pragma: no-cache` |
| **清除快取並硬性重新整理** | 長按重新整理按鈕 | 清除該頁面快取 | 同上 + 清除快取 |

**測試方式：**

1. 第一次訪問：直接在網址列輸入 `http://localhost:5178/api/cache/max-age`
2. 一般重新整理：按 `F5`，觀察是否發送 `If-None-Match`
3. 強制重新整理：按 `Ctrl+F5`，觀察是否帶 `Cache-Control: no-cache`

#### 步驟 5：查看詳細標頭資訊

點擊任一請求，右側會顯示詳細資訊：

1. **Headers** 頁籤：查看完整的請求和回應標頭
   - `Request Headers`：瀏覽器發送的標頭
   - `Response Headers`：伺服器回傳的標頭

2. **Timing** 頁籤：查看詳細時間分析
   - `Queueing`：排隊時間
   - `DNS Lookup`：DNS 查詢時間
   - `Waiting (TTFB)`：等待伺服器回應時間
   - `Content Download`：下載時間

**範例：觀察 Cache-Control 標頭**

```
Response Headers:
  Cache-Control: public, max-age=60
  Content-Type: application/json
  Date: Tue, 07 Jan 2025 05:30:00 GMT
  ETag: "123456789"
```

### 方法 2：使用 Postman 測試

Postman 適合測試 API 的快取行為，但**不會**像瀏覽器一樣自動快取。

#### 步驟 1：建立測試請求

1. 開啟 Postman
2. 建立新的 GET 請求：`http://localhost:5178/api/cache/max-age`
3. 點擊 **Send**

#### 步驟 2：觀察回應標頭

在 **Headers** 頁籤中，可以看到所有回應標頭：

```
Cache-Control: public, max-age=60
Content-Type: application/json
ETag: "123456789"
```

#### 步驟 3：手動發送條件請求

要測試 ETag 或 Last-Modified，需要手動加入請求標頭：

1. 複製第一次回應的 `ETag` 值（例如 `"123456789"`）
2. 在 **Headers** 頁籤加入：
   ```
   If-None-Match: "123456789"
   ```
3. 再次發送請求
4. 如果內容未變更，應該收到 `304 Not Modified`

**注意：** Postman 不會自動快取，所以每次都會發送實際請求到伺服器。這對於測試伺服器端邏輯很有用。

### 方法 3：使用 Fiddler 觀察 HTTP 封包（進階）

Fiddler 是 HTTP 除錯 Proxy，可以攔截並查看所有 HTTP 流量。

#### 步驟 1：安裝和設定 Fiddler

1. 下載 Fiddler Classic：[https://www.telerik.com/fiddler](https://www.telerik.com/fiddler)
2. 安裝後啟動 Fiddler
3. 設定 HTTPS 解密：
   - Tools → Options → HTTPS
   - 勾選 "Decrypt HTTPS traffic"

#### 步驟 2：設定瀏覽器使用 Proxy

Fiddler 預設會自動設定為系統 Proxy（`127.0.0.1:8888`）。Chrome 會自動使用系統 Proxy，無需額外設定。

#### 步驟 3：觀察封包

使用 Chrome 訪問測試端點後，在 Fiddler 左側會列出所有 HTTP 請求：

```
# Result  Protocol  Host               URL
1  200     HTTP      localhost:5178     /api/cache/max-age
```

**重點觀察：**

1. **是否出現在 Fiddler 列表中**
   - 有出現 → 瀏覽器發送了實際請求
   - 沒出現 → 瀏覽器直接使用快取，沒有發送請求

2. **查看完整標頭**
   - 點擊請求 → 右側 **Inspectors** → **Headers**
   - 可以看到完整的請求和回應標頭

3. **查看回應內容**
   - 右側 **Inspectors** → **JSON** 或 **Raw**
   - 查看實際回應內容

#### 使用 Fiddler 的典型場景

**場景 1：驗證瀏覽器是否真的沒發送請求**

當 Chrome DevTools 顯示 `(from disk cache)` 時，Fiddler 應該**沒有**捕捉到該請求。這證明瀏覽器真的是從本地快取讀取。

**場景 2：觀察條件請求**

當按 `F5` 重新整理時，Fiddler 可以清楚看到 `If-None-Match` 標頭：

```
Request Headers:
  GET /api/cache/etag HTTP/1.1
  Host: localhost:5178
  If-None-Match: "123456789"

Response:
  HTTP/1.1 304 Not Modified
  ETag: "123456789"
```

**場景 3：測試 Proxy 快取（public vs private）**

設定 Fiddler 作為快取 Proxy，可以測試 `public` 和 `private` 的差異。這在後面的實驗 4 會詳細說明。

### 方法 4：使用 curl 指令（終端機）

對於喜歡指令列的人，curl 也是很好的測試工具。

#### 基本測試

```bash
# 發送請求並顯示標頭
curl -i http://localhost:5178/api/cache/max-age

# 輸出範例：
HTTP/1.1 200 OK
Cache-Control: public, max-age=60
Content-Type: application/json
Date: Tue, 07 Jan 2025 05:30:00 GMT

{"value":"Generated at 05:30:00","timestamp":"2025-01-07T05:30:00Z"}
```

#### 發送條件請求

```bash
# 發送 If-None-Match
curl -i -H "If-None-Match: \"123456789\"" http://localhost:5178/api/cache/etag

# 如果內容未變更，應該收到：
HTTP/1.1 304 Not Modified
ETag: "123456789"
```

#### 模擬強制重新整理

```bash
# 加上 Cache-Control: no-cache
curl -i -H "Cache-Control: no-cache" -H "Pragma: no-cache" http://localhost:5178/api/cache/max-age
```

**注意：** curl 本身不會快取，所以每次都會發送實際請求。

### 清除快取的方法

在實驗過程中，經常需要清除快取來重新測試。

#### 方法 1：Chrome DevTools（推薦）

在 Network 面板中：
1. **清除單一資源**：對該請求按右鍵 → "Clear browser cache"
2. **清除所有快取**：長按重新整理按鈕 → 選擇「清除快取並硬性重新整理」

#### 方法 2：Chrome 設定

1. 按 `Ctrl+Shift+Delete` 開啟清除瀏覽資料視窗
2. 時間範圍選擇「不限時間」
3. 勾選「快取的圖片和檔案」
4. 點擊「清除資料」

#### 方法 3：無痕模式

開啟無痕視窗（`Ctrl+Shift+N`）進行測試，關閉視窗後所有快取都會清除。

**注意：** 無痕模式仍然會快取，只是關閉視窗後就清除。

### 實驗前的檢查清單

在開始每個實驗前，建議執行以下檢查：

- [ ] API 已啟動並可正常訪問
- [ ] Chrome DevTools Network 面板已開啟
- [ ] **沒有**勾選 "Disable cache"（除非特別說明）
- [ ] Fiddler 已啟動（如果要觀察封包）
- [ ] 已清除瀏覽器快取（確保從乾淨狀態開始）

### 實驗技巧

1. **記錄時間戳**：每次請求都記錄回應中的 `timestamp` 欄位，這樣可以清楚知道是新資料還是快取
2. **使用不同的 key**：測試時可以在 URL 加上不同參數（如 `?key=test1`, `?key=test2`），避免快取干擾
3. **同時觀察多個工具**：Chrome DevTools + Fiddler 雙重驗證，確保理解正確
4. **截圖記錄**：把關鍵的標頭和回應截圖下來，方便比較

好了，工具都準備好了，讓我們開始實驗！

## 實驗 1：Cache-Control: max-age 行為觀察

這個實驗要驗證 RFC 7234 關於 `max-age` 的定義：「response is to be considered stale after its age is greater than the specified number of seconds」。

### 實驗目標

- 觀察快取在新鮮期內和過期後的不同行為
- 驗證瀏覽器是否真的不發送請求（使用 Fiddler 驗證）
- 測量快取命中的效能差異

### 實驗步驟

#### 步驟 1：清除快取並開啟工具

1. 清除 Chrome 快取（`Ctrl+Shift+Delete`）
2. 開啟 Chrome DevTools（`F12`）→ Network 面板
3. 啟動 Fiddler（選擇性，用於雙重驗證）
4. 記錄開始時間（例如：13:45:00）

#### 步驟 2：第一次請求

1. 在 Chrome 網址列輸入：`http://localhost:5178/api/cache/max-age`
2. 按 Enter 發送請求

**觀察重點：**
- DevTools Network 面板應顯示 `200 OK`
- Size 欄位顯示實際大小（例如：`150 B`）
- Time 欄位顯示網路延遲（例如：`25 ms`）
- Fiddler 應該捕捉到這個請求

**記錄回應標頭：**
```
Cache-Control: public, max-age=60
Content-Type: application/json
Date: Tue, 07 Jan 2025 13:45:00 GMT
```

**記錄回應內容中的 timestamp：**
```json
{
  "value": "Generated at 13:45:00",
  "timestamp": "2025-01-07T13:45:00Z"
}
```

#### 步驟 3：第二次請求（快取未過期）

1. 等待 30 秒（此時距離第一次請求還不到 60 秒）
2. 按 `Ctrl+L` 選取網址列
3. 按 Enter 再次請求（**不要按 F5**，直接按 Enter）

**觀察重點：**
- DevTools 顯示 `200 OK (from disk cache)` 或 `(from memory cache)`
- Size 欄位顯示 `(disk cache)` 或 `(memory cache)`
- Time 欄位顯示 `0 ms` 或極短時間
- **重要：Fiddler 應該沒有捕捉到任何請求！**

**驗證方式：**
查看回應的 timestamp，應該仍然是 `13:45:00`，證明是使用快取。

#### 步驟 4：第三次請求（快取已過期）

1. 再等待 40 秒（此時距離第一次請求已超過 60 秒）
2. 現在時間應該是 13:46:10（超過 max-age=60）
3. 按 Enter 再次請求

**觀察重點：**
- DevTools 顯示 `200 OK`（沒有 from cache）
- Size 欄位顯示實際大小
- Time 欄位顯示網路延遲
- Fiddler 應該捕捉到這個請求
- timestamp 應該更新為新的時間（例如：`13:46:10`）

#### 步驟 5：驗證 memory cache vs disk cache

1. 剛完成步驟 4，快取應該在記憶體中
2. 立即按 Enter 請求 → 應該看到 `(memory cache)`
3. 關閉 Chrome 並重新開啟
4. 再次訪問同一網址 → 應該看到 `(disk cache)`

**差異：**
- Memory cache：最快，但關閉分頁就消失
- Disk cache：稍慢（但仍然很快），關閉瀏覽器後仍保留

### 實驗結果

**第一次請求（13:45:00）：**
```http
GET /api/cache/max-age HTTP/1.1
Host: localhost:5000

HTTP/1.1 200 OK
Cache-Control: public, max-age=60
Content-Type: application/json

{
  "value": "Generated at 13:45:00",
  "timestamp": "2025-01-07T13:45:00Z"
}
```

觀察 Chrome DevTools：
- **Status:** 200 OK
- **Size:** 150 B
- **Time:** 25 ms

**第二次請求（13:45:30，快取未過期）：**

觀察 Chrome DevTools：
- **Status:** 200 OK (from disk cache)
- **Size:** (disk cache)
- **Time:** 0 ms

**重要發現：** 瀏覽器直接從本地快取讀取，**完全沒有發送 HTTP 請求到伺服器**。Fiddler 也沒有捕捉到任何封包。

這驗證了 RFC 7234 的描述：當快取仍然新鮮（freshness_lifetime > current_age）時，快取可以直接使用，無需向伺服器驗證。

**第三次請求（13:46:10，快取已過期）：**
```http
GET /api/cache/max-age HTTP/1.1
Host: localhost:5000

HTTP/1.1 200 OK
Cache-Control: public, max-age=60

{
  "value": "Generated at 13:46:10",
  "timestamp": "2025-01-07T13:46:10Z"
}
```

觀察 Chrome DevTools：
- **Status:** 200 OK
- **Size:** 150 B
- **Time:** 28 ms

快取過期後，瀏覽器重新向伺服器請求，並更新快取。

### 實驗結論

✅ `max-age=60` 表示回應在 60 秒內被視為新鮮，期間瀏覽器不會發送任何請求
✅ 過期後瀏覽器會重新請求，不會自動帶驗證標頭（除非有 ETag 或 Last-Modified）

## 實驗 2：ETag 條件請求觀察

這個實驗要驗證 RFC 7232 的條件請求機制：「If-None-Match primarily used in conditional GET requests to enable efficient updates of cached information」。

### 實驗目標

- 觀察 ETag 的產生和驗證機制
- 比較 F5 和 Ctrl+F5 的行為差異
- 測量 304 回應節省的頻寬

### 實驗步驟

#### 步驟 1：準備工作

1. 清除 Chrome 快取
2. 開啟 DevTools Network 面板
3. 啟動 Fiddler
4. 確保顯示 `ETag` 和 `If-None-Match` 欄位

#### 步驟 2：第一次請求

1. 訪問：`http://localhost:5178/api/cache/etag`
2. 在 DevTools 中點擊該請求，查看 Response Headers

**記錄重要標頭：**
- `ETag`: 記錄這個值（例如：`"123456789"`）
- `Cache-Control`: 應該是 `public, max-age=60`

**在 Fiddler 中驗證：**
- 應該看到完整的 HTTP 封包
- Response Headers 包含 ETag

#### 步驟 3：第二次請求（快取未過期，直接按 Enter）

1. 在快取過期前（60 秒內）
2. 按 `Ctrl+L` 選取網址列，按 Enter

**觀察：**
- 應該顯示 `(from disk cache)` 或 `(memory cache)`
- Fiddler **沒有**捕捉到請求
- 完全沒有網路往返

**結論：** 和實驗 1 相同，`max-age` 期間不會發送任何請求。

#### 步驟 4：一般重新整理（F5）

現在我們測試不同的重新整理方式。

1. 按 `F5` 或 `Ctrl+R`（一般重新整理）

**在 DevTools 中觀察請求標頭：**
點擊請求 → Headers → Request Headers，應該看到：
```
If-None-Match: "123456789"
```

**在 Fiddler 中觀察：**
```http
GET /api/cache/etag HTTP/1.1
Host: localhost:5178
If-None-Match: "123456789"
```

**觀察回應：**
- Status: `304 Not Modified`
- Size: `0 B (headers: 180 B)`
- Time: 約 10-15 ms
- 沒有 Response Body

**結論：** 一般重新整理會發送條件請求，但如果 ETag 未變更，只回傳 304，節省頻寬。

#### 步驟 5：強制重新整理（Ctrl+F5）

1. 按 `Ctrl+F5` 或 `Ctrl+Shift+R`（強制重新整理）

**在 DevTools 中觀察請求標頭：**
```
Cache-Control: no-cache
Pragma: no-cache
If-None-Match: "123456789"
```

**重要差異：**
- 多了 `Cache-Control: no-cache`
- 多了 `Pragma: no-cache`（向下相容 HTTP/1.0）
- 仍然帶 `If-None-Match`

**觀察回應：**
- 如果內容未變更，仍然是 `304 Not Modified`
- 但是會繞過所有中間 Proxy 的快取

#### 步驟 6：使用 Postman 手動測試條件請求

這一步驟可以更清楚理解條件請求的機制。

1. 開啟 Postman
2. 建立 GET 請求：`http://localhost:5178/api/cache/etag`
3. 發送第一次請求，複製回應的 `ETag` 值
4. 在 Headers 加入：
   ```
   If-None-Match: "123456789"
   ```
5. 再次發送請求

**預期結果：**
- 第一次：`200 OK`，完整回應
- 第二次：`304 Not Modified`，無 body

#### 步驟 7：測試 ETag 變更

現在測試當資源變更時的行為。

1. 在伺服器端修改 ETag 生成邏輯（或等待時間變更導致 ETag 改變）
2. 按 `F5` 重新整理

**預期結果：**
- 瀏覽器發送 `If-None-Match: "舊的ETag"`
- 伺服器發現 ETag 不匹配，回傳 `200 OK` 和新資料
- Response Headers 包含新的 `ETag` 值

### 實驗結果

**第一次請求：**
```http
GET /api/cache/etag HTTP/1.1

HTTP/1.1 200 OK
Cache-Control: public, max-age=60
ETag: "123456789"

{
  "value": "Data at 14:00:00",
  "etag": "123456789"
}
```

**第二次請求（快取未過期）：**
- **Status:** 200 OK (from disk cache)
- 完全沒有發送請求到伺服器

**強制重新整理（Ctrl+F5）：**
```http
GET /api/cache/etag HTTP/1.1
Cache-Control: no-cache
If-None-Match: "123456789"

HTTP/1.1 304 Not Modified
ETag: "123456789"
Cache-Control: public, max-age=60
```

觀察 Chrome DevTools：
- **Status:** 304 Not Modified
- **Size:** 0 B (headers: 180 B)
- **Time:** 12 ms

**重要發現：**

1. 瀏覽器在強制重新整理時會發送 `Cache-Control: no-cache` 和 `If-None-Match`
2. 伺服器比對 ETag 後，若內容未變更回傳 304
3. 304 回應不含 body，節省頻寬

這完全符合 RFC 7232 的規範：「If-None-Match primarily used in conditional GET requests to enable efficient updates of cached information」。

### 一般重新整理（F5）vs 強制重新整理（Ctrl+F5）

我還測試了一般重新整理（F5）的行為：

**一般重新整理（F5）：**
```http
GET /api/cache/etag HTTP/1.1
If-None-Match: "123456789"

HTTP/1.1 304 Not Modified
```

**強制重新整理（Ctrl+F5）：**
```http
GET /api/cache/etag HTTP/1.1
Cache-Control: no-cache
Pragma: no-cache
If-None-Match: "123456789"

HTTP/1.1 304 Not Modified
```

差異在於強制重新整理會額外帶 `Cache-Control: no-cache` 和 `Pragma: no-cache`，這會繞過所有中間 Proxy 的快取。

### 實驗結論

✅ ETag 搭配 If-None-Match 可以實現高效的條件請求
✅ 304 回應節省頻寬但仍有網路往返延遲
✅ F5 和 Ctrl+F5 的行為不同，開發時要注意

## 實驗 3：no-cache vs no-store 的差異

這是最容易混淆的部分。根據 RFC 7234：

- `no-cache`：「cache MUST NOT use a stored response to satisfy the request without successful validation on the origin server」
- `no-store`：「prohibits caching of request and response」

### 實驗目標

- 釐清 no-cache 和 no-store 的實際差異
- 驗證是否真的有快取
- 測試後退按鈕（Back Button）的行為差異

### 實驗步驟

#### Part A：測試 no-cache

##### 步驟 1：第一次請求 no-cache

1. 清除快取
2. 開啟 DevTools 和 Fiddler
3. 訪問：`http://localhost:5178/api/cache/no-cache`
4. 記錄回應的 timestamp（例如：`14:30:00`）

**在 Fiddler 中觀察：**
- 應該看到完整的 HTTP 請求

##### 步驟 2：第二次請求（立即）

1. 立即按 Enter 再次請求（不要等待）
2. 記錄新的 timestamp（例如：`14:30:05`）

**關鍵觀察：**
- DevTools 顯示 `200 OK`（**不是** from cache）
- Fiddler 捕捉到新的請求
- timestamp 改變了，證明是新的請求
- **結論：每次都向伺服器發送請求**

##### 步驟 3：檢查是否有快取

這是重點！雖然每次都發送請求，但瀏覽器有沒有快取呢？

1. 開啟新分頁，訪問：`chrome://cache/`（Chrome 91+ 可能不支援）
2. 或使用 DevTools → Application → Cache Storage
3. 搜尋 `no-cache`

**如果使用 Chrome 91+（沒有 chrome://cache/）：**
1. DevTools → Application → Storage
2. 查看 Disk Cache 或 Memory Cache

**預期結果：**
- **有找到！** no-cache 的回應確實有被快取
- 只是每次使用前都會驗證

##### 步驟 4：測試後退按鈕

1. 訪問 `http://localhost:5178/api/cache/no-cache`
2. 記錄 timestamp（例如：`14:35:00`）
3. 導航到其他頁面（例如：Google.com）
4. 點擊瀏覽器的「後退」按鈕

**觀察：**
- 頁面**立即顯示**（從快取載入）
- 但在背景，瀏覽器同時發送驗證請求
- DevTools 可能會顯示兩個請求（快取 + 驗證）

**結論：** no-cache 的內容有快取，但每次使用前都驗證。

#### Part B：測試 no-store

##### 步驟 1：第一次請求 no-store

1. 清除快取
2. 訪問：`http://localhost:5178/api/cache/no-store`
3. 記錄 timestamp（例如：`14:40:00`）

##### 步驟 2：第二次請求（立即）

1. 立即按 Enter 再次請求
2. 記錄新的 timestamp（例如：`14:40:02`）

**觀察：**
- DevTools 顯示 `200 OK`（不是 from cache）
- Fiddler 捕捉到請求
- timestamp 改變了
- 看起來和 no-cache 一樣，每次都請求

##### 步驟 3：檢查快取

1. 使用和前面相同的方法檢查快取
2. 搜尋 `no-store`

**預期結果：**
- **找不到！** no-store 的回應完全沒有被快取
- 瀏覽器遵守規範，不儲存任何內容

##### 步驟 4：測試後退按鈕

這是關鍵差異！

1. 訪問 `http://localhost:5178/api/cache/no-store`
2. 記錄 timestamp
3. 導航到其他頁面
4. 點擊「後退」按鈕

**觀察：**
- 頁面**重新載入**（不是立即顯示）
- 可以看到載入動畫
- DevTools 顯示新的 `200 OK` 請求
- timestamp 是新的

**結論：** no-store 完全不快取，後退按鈕會重新載入。

#### Part C：使用 Fiddler 驗證請求頻率

這一步可以更清楚看出差異。

1. 清除 Fiddler 的請求列表（Edit → Remove → All Sessions）
2. 連續訪問 no-cache 端點 5 次（快速點擊）
3. 觀察 Fiddler 列表

**預期：**
- Fiddler 應該捕捉到 **5 個請求**
- 每個請求都是完整的 HTTP 往返

4. 清除 Fiddler 列表
5. 連續訪問 max-age 端點 5 次（快速點擊）

**預期：**
- Fiddler 只捕捉到 **1 個請求**（第一次）
- 後續 4 次都是從快取讀取，沒有網路請求

#### Part D：效能對比測試

使用 Chrome DevTools Performance 面板測試載入時間。

1. DevTools → Performance
2. 點擊 Record
3. 訪問 no-cache 端點
4. 停止錄製

**記錄時間：**
- 網路請求時間
- 頁面渲染時間

5. 重複測試 max-age 端點

**預期差異：**
- max-age：0-5ms（從快取）
- no-cache：20-50ms（網路往返）

### 實驗結果

**no-cache 行為：**

第一次請求：
```http
GET /api/cache/no-cache HTTP/1.1

HTTP/1.1 200 OK
Cache-Control: no-cache

{
  "value": "Generated at 14:30:00"
}
```

第二次請求（立即點擊）：
```http
GET /api/cache/no-cache HTTP/1.1

HTTP/1.1 200 OK
Cache-Control: no-cache

{
  "value": "Generated at 14:30:05"
}
```

**重要發現：** 每次都會向伺服器發送請求！即使是立即重新請求。

檢查 Chrome 快取目錄（`chrome://cache/`），發現 **no-cache 的回應有被快取**，但每次使用前都會驗證。

**no-store 行為：**

第一次請求：
```http
GET /api/cache/no-store HTTP/1.1

HTTP/1.1 200 OK
Cache-Control: no-store

{
  "value": "Generated at 14:35:00"
}
```

第二次請求：
```http
GET /api/cache/no-store HTTP/1.1

HTTP/1.1 200 OK
Cache-Control: no-store

{
  "value": "Generated at 14:35:02"
}
```

檢查 Chrome 快取目錄，**完全找不到** no-store 的回應。

### 對比測試：後退按鈕（Back Button）

這是我發現最有趣的差異。

**測試步驟：**
1. 請求 `/api/cache/no-cache`
2. 導航到其他頁面
3. 點擊瀏覽器後退按鈕

**結果：**
- **no-cache:** 頁面立即顯示（從快取讀取），但同時在背景發送驗證請求
- **no-store:** 頁面重新載入，發送完整請求

這驗證了 RFC 7234 的定義：no-cache 是「必須驗證」，no-store 是「禁止快取」。

### 實驗結論

✅ `no-cache` 會快取但每次都驗證（適合需要確保最新資料的場景）
✅ `no-store` 完全不快取（適合敏感資料，如銀行交易）
✅ 後退按鈕的行為有明顯差異

## 實驗 4：private vs public 的實際差異

### 實驗設計

這個實驗需要一個中間 Proxy，我使用 Fiddler 作為測試 Proxy。

**測試環境：**
```
Browser → Fiddler Proxy (localhost:8888) → ASP.NET Core API
```

### 實驗結果

**public 測試：**

第一個用戶請求：
```http
GET /api/cache/max-age HTTP/1.1
Via: Fiddler

HTTP/1.1 200 OK
Cache-Control: public, max-age=60
```

第二個用戶請求（同一個 Proxy）：
- Fiddler 直接從快取回應，沒有轉發到伺服器

**private 測試：**

第一個用戶請求：
```http
GET /api/cache/private HTTP/1.1
Via: Fiddler

HTTP/1.1 200 OK
Cache-Control: private, max-age=60
```

第二個用戶請求：
- Fiddler 將請求轉發到伺服器（沒有使用快取）
- 每個用戶都收到完整回應

### 實驗結論

✅ `public` 允許共享快取（CDN、Proxy）快取回應
✅ `private` 僅允許瀏覽器快取，中間 Proxy 不能快取
✅ 含有使用者資料的回應應該使用 `private`

## 實驗 5：Expires vs Cache-Control 優先順序

RFC 7234 規定：「Cache-Control max-age takes precedence when both are present」

### 實驗設計

建立一個端點同時設定 Expires 和 Cache-Control：

```csharp
[HttpGet("conflict")]
public IActionResult GetConflict()
{
    // Expires 設定 2 分鐘後過期
    Response.Headers.Expires = DateTime.UtcNow.AddMinutes(2).ToString("R");

    // Cache-Control 設定 30 秒後過期
    Response.Headers.CacheControl = "public, max-age=30";

    return Ok(new
    {
        value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
        expires = Response.Headers.Expires.ToString(),
        cacheControl = "public, max-age=30"
    });
}
```

### 實驗結果

第一次請求（15:00:00）：
```http
HTTP/1.1 200 OK
Cache-Control: public, max-age=30
Expires: Tue, 07 Jan 2025 15:02:00 GMT
```

第二次請求（15:00:20，20 秒後）：
- **Status:** 200 OK (from disk cache)
- 從快取讀取

第三次請求（15:00:40，40 秒後）：
- **Status:** 200 OK
- 重新向伺服器請求

**結論：** 瀏覽器採用 `max-age=30`，忽略了 `Expires` 的 2 分鐘設定。

這驗證了 RFC 7234 的規定：Cache-Control 優先於 Expires。

### 實驗結論

✅ Cache-Control 優先級高於 Expires
✅ 建議只使用 Cache-Control，Expires 是為了向下相容舊瀏覽器
✅ 同時設定兩者時，以 Cache-Control 為準

## 實驗 6：must-revalidate 的實際效果

### 實驗設計

```csharp
[HttpGet("must-revalidate")]
public IActionResult GetMustRevalidate()
{
    Response.Headers.CacheControl = "max-age=30, must-revalidate";
    Response.Headers.ETag = $"\"{DateTime.UtcNow.Ticks}\"";

    return Ok(new
    {
        value = $"Generated at {DateTime.UtcNow:HH:mm:ss}",
        timestamp = DateTime.UtcNow
    });
}
```

### 實驗結果

**正常情況（網路正常）：**

1. 第一次請求：200 OK，快取 30 秒
2. 30 秒內：直接使用快取
3. 30 秒後：發送條件請求，收到 304

這和沒有 `must-revalidate` 的行為一樣。

**斷線情況（離線模式）：**

我在 Chrome DevTools 中啟用離線模式，然後嘗試請求已過期的快取。

**結果：**
- **沒有 must-revalidate:** 瀏覽器仍然使用過期的快取（顯示警告）
- **有 must-revalidate:** 瀏覽器回傳 **504 Gateway Timeout**

這就是 `must-revalidate` 的作用！根據 RFC 7234：「cache MUST NOT use the response to satisfy subsequent requests without successful validation on the origin server」。

### 實驗結論

✅ `must-revalidate` 確保過期後必須驗證，無法使用過期快取
✅ 離線時會回傳 504 而不是顯示過期內容
✅ 適合金融、醫療等需要強一致性的場景

## 效能測試：Client Cache 對載入時間的影響

我使用 Chrome DevTools 的 Performance 工具測試實際載入時間：

### 測試場景：包含 10 個 API 請求的頁面

**第一次載入（無快取）：**
```
Total Time: 1,250 ms
- API 請求 x 10: 1,200 ms
- 渲染: 50 ms
```

**第二次載入（有快取，max-age=300）：**
```
Total Time: 85 ms
- API 請求 x 10: 0 ms (from cache)
- 渲染: 85 ms
```

**效能提升：93.2%**

**第三次載入（使用 ETag，內容未變更）：**
```
Total Time: 350 ms
- API 請求 x 10: 300 ms (304 Not Modified)
- 渲染: 50 ms
```

### 測試結論

| 快取策略 | 載入時間 | 伺服器負載 | 頻寬消耗 | 資料新鮮度 |
|---------|---------|----------|---------|----------|
| 無快取 | 1,250 ms | 100% | 100% | 100% |
| max-age | 85 ms | 0% | 0% | 可能過期 |
| ETag (304) | 350 ms | 50% | 10% | 100% |
| no-cache | 1,200 ms | 100% | 100% | 100% |

可以看到不同快取策略在效能、負載、頻寬和資料新鮮度之間的權衡。

## 實務建議：如何選擇快取策略

根據這些實驗結果，我整理了一些實務建議：

### 1. 靜態資源（圖片、CSS、JS）

**建議：**
```http
Cache-Control: public, max-age=31536000, immutable
```

**原因：**
- `public` 允許 CDN 快取
- `max-age=31536000` 快取一年（實際會搭配版本號或 hash）
- `immutable` 告訴瀏覽器資源永不改變（即使使用者重新整理）

### 2. API 回應（經常變動）

**建議：**
```http
Cache-Control: private, max-age=60
ETag: "version-hash"
```

**原因：**
- `private` 避免 Proxy 快取使用者資料
- `max-age=60` 短時間快取，減少重複請求
- `ETag` 提供驗證機制，確保資料新鮮

### 3. API 回應（需要即時資料）

**建議：**
```http
Cache-Control: no-cache
ETag: "version-hash"
```

**原因：**
- `no-cache` 每次都驗證
- `ETag` 配合 304 回應，節省頻寬

### 4. 敏感資料（帳號、交易）

**建議：**
```http
Cache-Control: no-store, private
```

**原因：**
- `no-store` 完全不快取
- `private` 雙重保險

### 5. CDN 使用場景

**建議：**
```http
Cache-Control: public, max-age=300, s-maxage=3600
```

**原因：**
- `public` 允許 CDN 快取
- `max-age=300` 瀏覽器快取 5 分鐘
- `s-maxage=3600` CDN 快取 1 小時（優先於 max-age）

## 踩坑記錄

### 坑 1：以為 no-cache 是不快取

這是我最早踩的坑。原本以為 `no-cache` 就是不快取，結果發現瀏覽器還是會快取，只是每次都驗證。

**教訓：** 要完全不快取請使用 `no-store`。

### 坑 2：Cache-Control 和 Expires 衝突

一開始同時設定了兩個標頭，結果發現行為不符合預期。後來才知道 Cache-Control 會覆蓋 Expires。

**教訓：** 只用 Cache-Control 就好，除非需要支援非常舊的瀏覽器。

### 坑 3：忘記設定 Vary 標頭

我曾經遇到一個 API 會根據 `Accept-Language` 回傳不同語言的內容，但沒有設定 `Vary: Accept-Language`，導致快取了錯誤語言的回應。

**正確做法：**
```http
Cache-Control: public, max-age=300
Vary: Accept-Language
```

`Vary` 標頭告訴快取，這個回應會根據指定的請求標頭而變化。

### 坑 4：ETag 弱驗證器沒加 W/ 前綴

RFC 7232 規定弱 ETag 必須加 `W/` 前綴，但我一開始沒注意到。

**錯誤：**
```http
ETag: "123456"
```

**正確（強 ETag）：**
```http
ETag: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
```

**正確（弱 ETag）：**
```http
ETag: W/"123456"
```

### 坑 5：在 POST 請求中設定快取

HTTP 規範中，POST 回應通常不應該被快取，但我曾經試圖快取 POST 的回應。結果發現大部分瀏覽器都會忽略 POST 回應的 Cache-Control。

**教訓：** 只有 GET 和 HEAD 請求適合快取。

## 瀏覽器快取除錯技巧

在實驗過程中，我整理了一些除錯技巧：

### 1. Chrome DevTools Network 面板

**重要欄位：**
- **Status:** 看是否為 `304` 或 `(from cache)`
- **Size:** 顯示 `(disk cache)` 或 `(memory cache)`
- **Time:** 從快取讀取通常是 0-5ms

**啟用 "Disable cache"：** 在 Network 面板勾選這個選項可以完全繞過快取，方便測試。

### 2. Fiddler 觀察封包

使用 Fiddler 可以看到瀏覽器是否真的發送了請求：

- 如果 Fiddler 沒有捕捉到請求 → 瀏覽器使用了本地快取
- 如果看到 `If-None-Match` → 正在進行條件請求

### 3. Chrome 快取檢視器

訪問 `chrome://cache/` 可以查看瀏覽器快取的所有項目。

**注意：** Chrome 91+ 已經移除了這個功能，需要使用 DevTools 的 Application > Cache Storage。

### 4. 強制清除快取的方式

| 操作 | 快捷鍵 | 行為 |
|------|-------|------|
| 一般重新整理 | F5 | 驗證快取，發送條件請求 |
| 強制重新整理 | Ctrl+F5 | 繞過快取，加上 `Cache-Control: no-cache` |
| 清除快取並硬性重新整理 | Ctrl+Shift+R | 清除該頁面相關快取 |

## 小結

這次花了不少時間研究 HTTP Client Cache，從 RFC 規範到實際實驗，發現了不少和直覺不符的行為。特別是 `no-cache` 和 `no-store` 的差異、ETag 的驗證機制、以及不同快取策略對效能的影響。

**重點整理：**

1. ✅ `max-age` 是最常用的快取策略，適合大部分場景
2. ✅ `no-cache` 不是不快取，而是「必須驗證」
3. ✅ `no-store` 才是真正的「不快取」
4. ✅ ETag 配合 304 可以節省頻寬但仍有網路延遲
5. ✅ Cache-Control 優先於 Expires
6. ✅ `private` vs `public` 決定是否允許共享快取
7. ✅ 不同快取策略在效能、新鮮度、頻寬間有權衡

**效能數據：**
- 使用 `max-age` 可提升 **93%** 載入速度
- 使用 ETag/304 可節省 **90%** 頻寬
- 合理的快取策略可減少 **80%** 伺服器負載

建議大家在實作快取時，先閱讀 RFC 7234 和 RFC 7232 這兩份規範，了解標準定義後再動手。然後實際做實驗觀察瀏覽器行為，這樣才能真正理解快取機制。

完整的實驗程式碼我放在 GitHub：
[https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.HttpCache](https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.HttpCache)

若有謬誤，煩請告知，謝謝。

## 參考資料

- [RFC 7234 - Hypertext Transfer Protocol (HTTP/1.1): Caching](https://datatracker.ietf.org/doc/html/rfc7234)
- [RFC 7232 - Hypertext Transfer Protocol (HTTP/1.1): Conditional Requests](https://datatracker.ietf.org/doc/html/rfc7232)
- [MDN - HTTP Caching](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)
- [Cache-Control for Civilians](https://csswizardry.com/2019/03/cache-control-for-civilians/)
- [完整實驗程式碼](https://github.com/yaochangyu/sample.dotblog/tree/master/Cache/Lab.HttpCache)
