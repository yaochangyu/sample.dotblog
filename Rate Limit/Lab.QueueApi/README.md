# Queued Web API

一個具有排隊機制的 ASP.NET Core 9 Web API，支援每分鐘最多處理 2 個請求，超出的請求進入隊列並返回 429 狀態碼。

## 功能特色

- **限流機制**: 使用滑動視窗演算法，每分鐘最多處理 2 個請求
- **排隊系統**: 超出限制的請求自動進入隊列等待處理
- **429 狀態碼**: 排隊請求返回 HTTP 429 (Too Many Requests) 狀態碼
- **重試機制**: 提供 Retry-After 標頭指示客戶端何時重試
- **背景處理**: 使用背景服務處理排隊的請求
- **狀態查詢**: 支援查詢排隊請求的處理狀態
- **健康檢查**: 提供系統健康狀態和佇列資訊

## 系統架構

### 核心組件

1. **SlidingWindowRateLimiter**: 滑動視窗限流器
   - 追蹤請求時間戳
   - 實現每分鐘 2 個請求的限制
   - 計算重試等待時間

2. **ChannelRequestQueue**: 請求佇列服務
   - 使用 .NET Channel 實現並發安全的佇列
   - 支援請求入隊和出隊操作
   - 管理請求完成狀態

3. **BackgroundRequestProcessor**: 背景處理服務
   - 持續監控佇列中的請求
   - 遵守限流規則處理排隊請求
   - 設定請求處理結果

4. **QueuedApiController**: API 控制器
   - 處理 HTTP 請求
   - 整合限流器和佇列服務
   - 提供多個端點支援不同操作

## API 端點

### 1. 處理請求 - POST /api/queuedapi/process

處理業務請求，支援限流和排隊機制。

**請求格式:**
```json
{
  "data": "your request data"
}
```

**成功回應 (200 OK):**
```json
{
  "success": true,
  "message": "Request processed successfully (direct)",
  "data": {
    "originalData": "your request data",
    "processedData": "Processed: your request data",
    "processingType": "Direct"
  },
  "processedAt": "2025-09-17T23:45:52.4024825Z"
}
```

**排隊回應 (429 Too Many Requests):**
```json
{
  "message": "Too many requests. Please retry after the specified time.",
  "requestId": "93e6c11d-e354-431d-880b-0da5b739bb68",
  "retryAfterSeconds": 57,
  "queuePosition": 1
}
```

**回應標頭:**
- `Retry-After`: 建議重試的秒數
- `X-Queue-Position`: 在佇列中的位置
- `X-Request-Id`: 請求唯一識別碼

### 2. 查詢狀態 - GET /api/queuedapi/status/{requestId}

查詢排隊請求的處理狀態。

**處理中回應 (202 Accepted):**
```json
{
  "message": "Request is still being processed",
  "requestId": "93e6c11d-e354-431d-880b-0da5b739bb68",
  "queuePosition": 1
}
```

**完成回應 (200 OK):**
```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "requestId": "93e6c11d-e354-431d-880b-0da5b739bb68",
    "originalData": "your request data",
    "queuedAt": "2025-09-17T23:45:53.0000000Z",
    "processedData": "Processed: your request data"
  },
  "processedAt": "2025-09-17T23:46:53.0000000Z"
}
```

### 3. 等待結果 - GET /api/queuedapi/wait/{requestId}

等待排隊請求完成並返回結果（最多等待 2 分鐘）。

**成功回應 (200 OK):**
```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "requestId": "93e6c11d-e354-431d-880b-0da5b739bb68",
    "originalData": "your request data",
    "queuedAt": "2025-09-17T23:45:53.0000000Z",
    "processedData": "Processed: your request data"
  },
  "processedAt": "2025-09-17T23:46:53.0000000Z"
}
```

### 4. 健康檢查 - GET /api/queuedapi/health

獲取系統健康狀態和佇列資訊。

**回應 (200 OK):**
```json
{
  "status": "Healthy",
  "queueLength": 0,
  "canAcceptRequest": true,
  "retryAfterSeconds": 0,
  "timestamp": "2025-09-17T23:47:08.4877436Z"
}
```

## 使用範例

### 基本使用流程

1. **發送請求**
```bash
curl -X POST -H "Content-Type: application/json" \
     -d '{"data":"Test request"}' \
     http://localhost:5001/api/queuedapi/process
```

2. **如果收到 429 回應，提取請求 ID 並等待**
```bash
# 查詢狀態
curl http://localhost:5001/api/queuedapi/status/{requestId}

# 或等待結果
curl http://localhost:5001/api/queuedapi/wait/{requestId}
```

3. **檢查系統健康狀態**
```bash
curl http://localhost:5001/api/queuedapi/health
```

### 客戶端重試邏輯範例

```csharp
public async Task<ApiResponse> SendRequestWithRetry(string data)
{
    var request = new { data = data };
    var response = await httpClient.PostAsJsonAsync("/api/queuedapi/process", request);
    
    if (response.StatusCode == HttpStatusCode.TooManyRequests)
    {
        // 提取請求 ID 和重試時間
        var queuedResponse = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(1);
        
        // 等待指定時間後重試
        await Task.Delay(retryAfter);
        
        // 使用請求 ID 獲取結果
        var resultResponse = await httpClient.GetAsync($"/api/queuedapi/wait/{queuedResponse.RequestId}");
        return await resultResponse.Content.ReadFromJsonAsync<ApiResponse>();
    }
    
    return await response.Content.ReadFromJsonAsync<ApiResponse>();
}
```

## 建置和執行

### 前置需求

- .NET 9.0 SDK
- ASP.NET Core 9.0

### 建置專案

```bash
dotnet build
```

### 執行專案

```bash
dotnet run
```

API 將在 `http://localhost:5001` 上啟動。

### 開發模式

在開發模式下，Swagger UI 將在根路徑 (`http://localhost:5001`) 提供 API 文件。

## 配置選項

### 限流設定

在 `Program.cs` 中可以調整限流參數：

```csharp
builder.Services.AddSingleton<IRateLimiter>(provider => 
    new SlidingWindowRateLimiter(
        maxRequests: 2,                    // 最大請求數
        timeWindow: TimeSpan.FromMinutes(1) // 時間視窗
    ));
```

### 佇列設定

調整佇列容量：

```csharp
builder.Services.AddSingleton<IRequestQueue>(provider => 
    new ChannelRequestQueue(capacity: 100)); // 佇列容量
```

## 測試

專案包含完整的測試腳本 `test_api.sh`，可以測試所有功能：

```bash
chmod +x test_api.sh
./test_api.sh
```

測試腳本將驗證：
- 健康檢查端點
- 直接處理請求（前 2 個請求）
- 排隊機制（第 3 個請求）
- 狀態查詢功能
- 背景處理功能

## 技術細節

### 限流演算法

使用滑動視窗計數器演算法：
- 維護請求時間戳的佇列
- 定期清理過期的時間戳
- 基於當前視窗內的請求數量決定是否允許新請求

### 並發安全

- 使用 `ConcurrentQueue` 和 `ConcurrentDictionary` 確保執行緒安全
- 使用 `Channel` 實現生產者-消費者模式
- 適當的鎖定機制保護共享資源

### 錯誤處理

- 完整的異常處理和日誌記錄
- 優雅的超時處理
- 適當的 HTTP 狀態碼回應

## 部署建議

1. **生產環境配置**
   - 移除 Swagger UI
   - 配置適當的日誌等級
   - 設定 HTTPS

2. **監控和警報**
   - 監控佇列長度
   - 追蹤處理時間
   - 設定限流觸發警報

3. **擴展性考慮**
   - 考慮使用 Redis 實現分散式限流
   - 使用訊息佇列（如 RabbitMQ）處理大量請求
   - 實現水平擴展支援

## 授權

此專案僅供學習和參考使用。

