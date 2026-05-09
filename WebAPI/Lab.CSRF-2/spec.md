# 角色定位
您是資深資安專家，協助分析與實作 Web API 防濫用機制，確保公開 API 的安全性。

# 目標
保護可匿名存取的 Web API，防止惡意濫用與攻擊。

# 防護機制設計

## 1. Token 取得流程
**端點**: `GET /api/token`

**回應規格**:
- Token 放置位置: Response Header (`X-CSRF-Token`)
- 回應本文: JSON `{ message, token }`
- Token 儲存位置: Server 端 `IMemoryCache`

**Token 特性**:
- 預設有效期限: 5 分鐘
- 使用次數限制: 可配置（預設 1 次）
- 唯一性: 每次請求產生不同 Token
- 綁定資訊: User-Agent、IP 位址（IP 驗證邏輯目前保留為可選）

## 2. 受保護端點存取流程
**端點**: `POST /api/protected`

**請求規格**:
- Token 放置位置: Request Header (`X-CSRF-Token`)
- Request Body: JSON `{"data":"..."}`

**驗證邏輯**:
1. 驗證 User-Agent 是否存在且不在黑名單
2. 驗證 Referer 是否在白名單（開發環境允許缺少）
3. 驗證 Origin 是否在白名單（僅 CORS 請求）
4. 比對 Request Header Token 與 Server 端儲存的 Token
5. 驗證 Token 有效期限與使用次數
6. 驗證 User-Agent 是否與申請 Token 時一致
7. 驗證成功 → HTTP 200 + 回傳結果
8. 驗證失敗：
   - Token 問題 → HTTP 401 Unauthorized
   - User-Agent / Referer / Origin 問題 → HTTP 403 Forbidden
   - 速率限制 → HTTP 429 Too Many Requests

## 3. 客戶端實作場景

### 場景 A: cURL 命令列工具
```bash
# 步驟 1: 取得 Token
# 步驟 2: 攜帶 Token 呼叫受保護端點
```

### 場景 B: 瀏覽器 (HTML + JavaScript)
```javascript
// 步驟 1: Fetch Token 並存放
// 步驟 2: 攜帶 Token 發送受保護請求
```

# 實作需求
- 使用 ASP.NET Core Web API
- 實作 Token 產生、驗證、過期機制
- 提供 cURL 與瀏覽器範例
- 包含安全性測試案例

## 目前實作參數
- API Base URL: `http://localhost:5073`
- HTTPS URL: `https://localhost:7026`
- Token 端點速率限制: 1 分鐘 5 次
- Protected API 速率限制: 10 秒 10 次
- Token 預設參數: `maxUsage=1`, `expirationMinutes=5`

# 預期產出
1. 實作計畫文件 (含步驟核取清單)
2. 完整可執行的程式碼
3. 客戶端呼叫範例 (cURL + HTML/JS)
4. 安全性驗證測試
