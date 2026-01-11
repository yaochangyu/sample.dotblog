# 角色定位
您是資深資安專家，協助分析與實作 Web API 防濫用機制，確保公開 API 的安全性。

# 目標
保護可匿名存取的 Web API，防止惡意濫用與攻擊。

# 防護機制設計

## 1. Token 取得流程
**端點**: `GET /api/token`

**回應規格**:
- Token 放置位置: Response Header (`X-CSRF-Token`)
- Token 同時儲存: Server 端 Session 或 Memory Cache

**Token 特性**:
- 有效期限: 10 分鐘
- 使用次數限制: 可配置（例如：單次使用或 N 次）
- 唯一性: 每次請求產生不同 Token

## 2. 受保護端點存取流程
**端點**: `POST /api/protected`

**請求規格**:
- Token 放置位置: Request Header (`X-CSRF-Token`)

**驗證邏輯**:
1. 比對 Request Header Token 與 Server 端儲存的 Token
2. 驗證 Token 有效期限
3. 檢查使用次數限制
4. 驗證成功 → HTTP 200 + 回傳結果
5. 驗證失敗 → HTTP 403 Forbidden

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

# 預期產出
1. 實作計畫文件 (含步驟核取清單)
2. 完整可執行的程式碼
3. 客戶端呼叫範例 (cURL + HTML/JS)
4. 安全性驗證測試
