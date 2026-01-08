# 快速開始指南

這份文件將協助您快速啟動並測試專案的多層快取功能。

## 前置需求

- .NET 9.0 SDK
- Docker Desktop（用於 Redis）
- curl 或 Postman（用於測試 API）

## 啟動步驟

### 1. 啟動 Redis

```bash
cd D:\lab\sample.dotblog\Cache\Lab.HttpCache
docker-compose up -d
```

驗證 Redis 是否正常運作：
```bash
docker ps
# 應該會看到 redis 容器正在運行
```

### 2. 啟動 Web API

```bash
cd src\Lab.HttpCache.Api
dotnet run
```

應該會看到類似以下的輸出：
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5178
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### 3. 開啟 API 文件

在瀏覽器中開啟：
- Swagger UI: http://localhost:5178/swagger
- 或直接訪問首頁：http://localhost:5178

## 快速測試腳本

### 測試 1：多層快取完整流程

```bash
# 1️⃣ 首次請求文章（會從資料庫讀取並快取）
curl -i http://localhost:5178/api/clientcache/article/1

# 回應中注意以下內容：
# - Cache-Control: public, max-age=60, must-revalidate
# - ETag: "某個數字"
# - requestId: 1（第一次請求）

# 2️⃣ 立即再次請求（HybridCache L1 記憶體快取命中，極快）
curl -i http://localhost:5178/api/clientcache/article/1

# 注意：requestId 會增加，但回應速度極快（~1ms）

# 3️⃣ 使用 ETag 進行條件請求（模擬 60 秒後瀏覽器的行為）
curl -i -H "If-None-Match: \"<從步驟1複製的ETag>\"" \
  http://localhost:5178/api/clientcache/article/1

# 回應：HTTP/1.1 304 Not Modified（無 body，節省流量！）

# 4️⃣ 更新文章（會清除快取）
curl -X PUT -H "Content-Type: application/json" \
  -d "{\"title\":\"更新的標題\",\"content\":\"更新的內容\"}" \
  http://localhost:5178/api/clientcache/article/1

# 注意新的 ETag 已改變

# 5️⃣ 使用舊的 ETag 再次請求（內容已變更）
curl -i -H "If-None-Match: \"<步驟1的舊ETag>\"" \
  http://localhost:5178/api/clientcache/article/1

# 回應：HTTP/1.1 200 OK + 完整的新資料（ETag 不匹配）
```

### 測試 2：快取統計與驗證

```bash
# 查看伺服器收到的總請求數
curl http://localhost:5178/api/clientcache/stats

# 回應範例：
# {
#   "totalRequests": 5,
#   "serverStartTime": "2026-01-08T01:23:45.678Z",
#   "uptime": "00:05:30.123"
# }

# 重置計數器
curl -X POST http://localhost:5178/api/clientcache/reset
```

### 測試 3：觀察不同 Cache-Control 指令的行為

```bash
# max-age（標準快取）
curl -i http://localhost:5178/api/clientcache/max-age

# no-store（完全禁止快取）
curl -i http://localhost:5178/api/clientcache/no-store

# immutable（永不改變）
curl -i http://localhost:5178/api/clientcache/immutable

# stale-while-revalidate（背景重新驗證）
curl -i http://localhost:5178/api/clientcache/stale-while-revalidate

# 查看所有端點
curl http://localhost:5178/api/clientcache/stats
```

### 測試 4：文章列表快取

```bash
# 取得所有文章（會快取 3 分鐘）
curl -i http://localhost:5178/api/clientcache/articles

# 再次請求（從快取讀取）
curl -i http://localhost:5178/api/clientcache/articles

# 更新任一文章（會清除列表快取）
curl -X PUT -H "Content-Type: application/json" \
  -d "{\"title\":\"新標題\",\"content\":\"新內容\"}" \
  http://localhost:5178/api/clientcache/article/1

# 再次取得文章列表（快取已清除，會重新查詢）
curl -i http://localhost:5178/api/clientcache/articles
```

## 使用 PowerShell 測試（Windows）

如果您使用 PowerShell，可以使用以下指令：

```powershell
# 首次請求
Invoke-WebRequest -Uri "http://localhost:5178/api/clientcache/article/1" -Method Get

# 帶標頭的條件請求
$headers = @{
    "If-None-Match" = "`"638734982345678901`""
}
Invoke-WebRequest -Uri "http://localhost:5178/api/clientcache/article/1" `
    -Method Get -Headers $headers

# 更新文章
$body = @{
    title = "更新的標題"
    content = "更新的內容"
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5178/api/clientcache/article/1" `
    -Method Put -Body $body -ContentType "application/json"
```

## 使用瀏覽器 DevTools 觀察

1. 開啟 Chrome DevTools（F12）
2. 切換到 **Network** 分頁
3. 勾選 **Disable cache**（先不勾選）
4. 訪問：http://localhost:5178/api/clientcache/article/1
5. 觀察回應標頭：
   - `Cache-Control: public, max-age=60, must-revalidate`
   - `ETag: "..."`
6. 在 60 秒內重新整理頁面
   - 狀態顯示：`200 OK (from disk cache)` 或 `(from memory cache)`
   - **Size** 欄位顯示：`(disk cache)` 或 `(memory cache)`
7. 等待 60 秒後重新整理
   - 狀態顯示：`304 Not Modified`
   - **Size** 欄位顯示實際傳輸的位元組數（很小，只有標頭）

## 效能比較實驗

執行以下腳本來觀察快取的效能提升：

```bash
# 測試無快取的效能（no-store）
time curl -s http://localhost:5178/api/clientcache/no-store > /dev/null

# 測試有快取的效能（首次請求）
time curl -s http://localhost:5178/api/clientcache/article/1 > /dev/null

# 測試有快取的效能（第二次請求，從 HybridCache 讀取）
time curl -s http://localhost:5178/api/clientcache/article/1 > /dev/null
```

預期結果：
- 第一次請求：~10-15ms（包含模擬資料庫延遲）
- 第二次請求：~1-3ms（從記憶體快取讀取）

## 查看 Redis 快取內容

```bash
# 連接到 Redis 容器
docker exec -it lab-httpCache-redis redis-cli

# 查看所有快取鍵
KEYS *

# 查看特定文章的快取
GET article:1

# 查看快取的 TTL（剩餘時間）
TTL article:1

# 退出
exit
```

## 常見端點一覽

| 端點 | 方法 | 說明 |
|------|------|------|
| `/api/clientcache/article/{id}` | GET | 取得單一文章（多層快取） |
| `/api/clientcache/article/{id}` | PUT | 更新文章（清除快取） |
| `/api/clientcache/articles` | GET | 取得所有文章 |
| `/api/clientcache/stats` | GET | 查看請求統計 |
| `/api/clientcache/reset` | POST | 重置計數器 |
| `/api/clientcache/max-age` | GET | 測試 max-age 指令 |
| `/api/clientcache/no-cache` | GET | 測試 no-cache 指令 |
| `/api/clientcache/no-store` | GET | 測試 no-store 指令 |
| `/api/clientcache/immutable` | GET | 測試 immutable 指令 |
| `/api/clientcache/stale-while-revalidate` | GET | 測試背景重新驗證 |

## 預設文章資料

專案包含 5 篇預設文章（ID 1-5）：
1. 深入探討 HTTP Client-Side Cache
2. ASP.NET Core 的 HybridCache 實戰
3. ETag 與條件請求的最佳實踐
4. CDN 與 s-maxage 的應用
5. stale-while-revalidate 優化使用者體驗

## 故障排除

### Redis 連線失敗

```bash
# 檢查 Redis 容器狀態
docker ps

# 查看 Redis 日誌
docker logs lab-httpCache-redis

# 重新啟動 Redis
docker-compose restart
```

### Port 5178 已被佔用

修改 `src/Lab.HttpCache.Api/Properties/launchSettings.json`：
```json
{
  "applicationUrl": "http://localhost:5179"  // 改成其他 port
}
```

### 快取未生效

1. 確認 Redis 正在運行
2. 檢查 `appsettings.json` 中的 Redis 連線字串
3. 查看應用程式日誌確認是否有錯誤訊息

## 進階測試

### 使用 JMeter 進行壓力測試

1. 建立測試計畫
2. 加入 HTTP Request Sampler
3. 設定 URL: `http://localhost:5178/api/clientcache/article/1`
4. 加入 View Results Tree 和 Summary Report
5. 執行測試並觀察：
   - 第一次請求的回應時間
   - 後續請求的回應時間（應該快很多）

### 監控 Redis 記憶體使用

```bash
# 進入 Redis CLI
docker exec -it lab-httpCache-redis redis-cli

# 查看記憶體資訊
INFO memory

# 查看快取統計
INFO stats
```

## 更多資訊

- 詳細架構說明：[CACHE-ARCHITECTURE.md](./CACHE-ARCHITECTURE.md)
- 技術文章：[blog-article.md](./blog-article.md)
- RFC 9111 規範：https://datatracker.ietf.org/doc/html/rfc9111

---

🎉 現在您已經準備好探索多層快取的威力了！
