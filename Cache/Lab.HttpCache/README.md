# Lab.HttpCache - 快取實作範例

這是一個完整的 ASP.NET Core 快取實作範例，展示三種不同層級的快取機制。

## 專案架構

```
Lab.HttpCache/
├── src/
│   └── Lab.HttpCache.Api/
│       ├── Controllers/
│       │   └── CacheController.cs          # 快取範例 API
│       ├── Services/
│       │   ├── IMemoryCacheService.cs      # Memory Cache 介面
│       │   ├── MemoryCacheService.cs       # Memory Cache 實作
│       │   ├── IRedisCacheService.cs       # Redis Cache 介面
│       │   ├── RedisCacheService.cs        # Redis Cache 實作
│       │   ├── ITwoLevelCacheService.cs    # 二級快取介面
│       │   └── TwoLevelCacheService.cs     # 二級快取實作
│       ├── Program.cs                      # 應用程式進入點
│       └── appsettings.json                # 配置檔
├── docker-compose.yml                      # Docker Compose 配置
└── README.md                               # 說明文件
```

## 快取機制

### 1. Memory Cache (一級快取)
- **說明**：使用應用程式記憶體儲存資料
- **優點**：速度最快
- **缺點**：容量受限於記憶體，應用程式重啟後資料消失
- **適用場景**：熱門資料、需要極快回應速度的場景

### 2. Redis Cache (二級快取)
- **說明**：使用 Redis 分散式快取儲存資料
- **優點**：可跨多個應用程式實例共享、持久化儲存
- **缺點**：速度較 Memory Cache 慢
- **適用場景**：需要跨實例共享的資料、需要持久化的快取

### 3. Two-Level Cache (二級快取策略)
- **說明**：結合 Memory Cache 和 Redis Cache
- **策略**：
  1. 讀取時先查 Memory Cache
  2. Memory Cache 未命中時查 Redis Cache
  3. Redis Cache 命中時回寫到 Memory Cache
  4. 寫入時同時寫入兩層快取
- **優點**：兼具速度與共享性
- **適用場景**：高併發且需要資料共享的場景

### 4. HTTP Cache (客戶端快取)
- **說明**：使用 HTTP 標頭控制瀏覽器或 CDN 快取
- **機制**：
  - **ResponseCache Attribute**：使用屬性設定快取策略
  - **Cache-Control**：手動設定快取控制標頭
  - **ETag**：使用實體標籤進行條件請求
- **優點**：減少伺服器負載、降低網路流量
- **適用場景**：靜態或不常變動的資料

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
- Swagger UI: `https://localhost:5001/swagger` (開發模式)
- OpenAPI 文件: `https://localhost:5001/openapi/v1.json`

## API 端點

### Memory Cache 範例
```bash
# 取得或建立快取資料
GET /api/cache/memory?key=memory-test

# 刪除快取
DELETE /api/cache/memory/memory-test
```

### Redis Cache 範例
```bash
# 取得或建立快取資料
GET /api/cache/redis?key=redis-test

# 刪除快取
DELETE /api/cache/redis/redis-test
```

### 二級快取範例
```bash
# 取得或建立快取資料
GET /api/cache/two-level?key=two-level-test

# 刪除快取
DELETE /api/cache/two-level/two-level-test
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
```

## 測試快取機制

### 測試 Memory Cache

```bash
# 第一次請求 - 建立快取
curl http://localhost:5000/api/cache/memory?key=test1

# 第二次請求 - 從快取讀取
curl http://localhost:5000/api/cache/memory?key=test1

# 刪除快取
curl -X DELETE http://localhost:5000/api/cache/memory/test1
```

### 測試 HTTP Cache (使用 curl)

```bash
# 第一次請求 - 取得 ETag
curl -i http://localhost:5000/api/cache/http-etag

# 第二次請求 - 使用 ETag 條件請求
curl -i -H "If-None-Match: \"<ETag值>\"" http://localhost:5000/api/cache/http-etag
# 應該回傳 304 Not Modified
```

### 測試二級快取

```bash
# 建立快取 (同時寫入 Memory 和 Redis)
curl http://localhost:5000/api/cache/two-level?key=shared-data

# 從快取讀取
curl http://localhost:5000/api/cache/two-level?key=shared-data
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

- **Redis.Configuration**: Redis 連線字串

## 快取過期時間

- **Memory Cache**: 5 分鐘
- **Redis Cache**: 10 分鐘
- **Two-Level Cache**: 10 分鐘
- **HTTP ResponseCache**: 60 秒
- **HTTP Cache-Control**: 120 秒
- **HTTP ETag**: 60 秒

## 技術堆疊

- **.NET 9.0**
- **ASP.NET Core Web API**
- **IMemoryCache** (內建)
- **IDistributedCache** (Microsoft.Extensions.Caching.StackExchangeRedis)
- **Redis 7 Alpine**
- **ResponseCache Middleware**

## 注意事項

1. **Redis 連線**：確保 Redis 服務正在執行，否則 Redis 和二級快取相關功能會失敗
2. **Memory Cache 限制**：Memory Cache 資料會在應用程式重啟後消失
3. **HTTP Cache 測試**：HTTP Cache 需要使用支援快取的客戶端 (如瀏覽器、curl) 才能觀察到效果
4. **ETag 實作**：此範例使用簡單的 HashCode 作為 ETag，生產環境建議使用更穩定的雜湊演算法

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

- [ASP.NET Core 記憶體快取](https://learn.microsoft.com/zh-tw/aspnet/core/performance/caching/memory)
- [ASP.NET Core 分散式快取](https://learn.microsoft.com/zh-tw/aspnet/core/performance/caching/distributed)
- [ASP.NET Core 回應快取](https://learn.microsoft.com/zh-tw/aspnet/core/performance/caching/response)
- [HTTP 快取標頭](https://developer.mozilla.org/zh-TW/docs/Web/HTTP/Caching)
