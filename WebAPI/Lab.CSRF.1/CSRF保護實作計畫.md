# CSRF 保護實作計畫

## 目標
建立一個 ASP.NET Core Web API，實作多層次的 CSRF（跨站請求偽造）保護機制，確保 API 只能被指定的前端頁面呼叫。

## 實作步驟

### ☐ 1. 建立 ASP.NET Core Web API 專案
**為什麼需要：** 建立基礎專案結構，作為後續開發的基礎。
- 使用 .NET 8 或 .NET 9
- 選擇 Web API 範本
- 設定專案基本配置

### ☐ 2. 實作 CSRF Token 服務
**為什麼需要：** 產生和驗證 CSRF Token 是防護的核心機制。
- 建立 Token 產生器
- 實作 Token 儲存機制（使用分散式快取或記憶體快取）
- 實作 Token 驗證邏輯
- 設定 Token 過期時間

### ☐ 3. 建立 CSRF 驗證 Filter
**為什麼需要：** 自動攔截所有需要保護的 API 請求，進行 Token 驗證。
- 建立 ActionFilter 或 Middleware
- 從 Header 或 Cookie 中取得 Token
- 呼叫驗證服務進行檢查
- 驗證失敗時回傳 403 Forbidden

### ☐ 4. 建立測試用的 Controller
**為什麼需要：** 提供實際的 API 端點來測試 CSRF 保護機制。
- 建立取得 Token 的端點（GET）
- 建立需要保護的端點（POST/PUT/DELETE）
- 套用 CSRF 驗證 Filter

### ☐ 5. 設定 CORS 和 Cookie 政策
**為什麼需要：** 限制只有特定網域可以呼叫 API，並正確設定 Cookie 屬性。
- 設定 CORS 允許特定來源
- 設定 Cookie 的 SameSite 屬性
- 設定 Cookie 的 HttpOnly 和 Secure 屬性
- 允許前端讀取特定 Header

### ☐ 6. 建立前端測試頁面
**為什麼需要：** 驗證整個流程是否正常運作。
- 建立 HTML 頁面
- 實作取得 Token 的邏輯
- 實作帶 Token 呼叫 API 的邏輯
- 建立測試案例（正常請求、缺少 Token、錯誤 Token）

### ☐ 7. 建立 README 說明文件
**為什麼需要：** 記錄專案的使用方式和保護機制的運作原理。
- 說明 CSRF 防護機制
- 說明如何執行專案
- 說明如何測試
- 列出安全性考量

## 防護機制說明

### 主要防護層次
1. **Anti-CSRF Token**：每個頁面都有唯一的 Token，必須在請求時帶上
2. **CORS 限制**：只允許特定網域的請求
3. **SameSite Cookie**：防止第三方網站發送 Cookie
4. **自訂 Header**：利用瀏覽器同源政策，跨站無法設定自訂 Header

### 安全性考量
- Token 應該有時效性
- Token 應該與 Session 綁定
- 使用 HTTPS 傳輸
- Cookie 設定 HttpOnly 和 Secure
