# CSRF 防護實作計畫

## 目標
實作 Anti-Forgery Token 機制，確保 Web API 只能被當前頁面使用，防止跨站請求偽造攻擊。

## 實作步驟

- [ ] **步驟 1: 建立 ASP.NET Core Web API 專案**
  - 原因：需要一個基礎專案來實作 CSRF 防護機制
  - 內容：使用 `dotnet new webapi` 建立專案框架

- [ ] **步驟 2: 設定 Anti-Forgery 服務**
  - 原因：ASP.NET Core 內建 Anti-Forgery 功能，需要在 DI 容器中註冊
  - 內容：在 `Program.cs` 中加入 `AddAntiforgery()` 服務設定

- [ ] **步驟 3: 建立 Token 產生端點**
  - 原因：前端需要取得 Token 才能發送受保護的請求
  - 內容：建立 API 端點產生並回傳 Anti-Forgery Token

- [ ] **步驟 4: 建立受保護的 API 端點**
  - 原因：示範如何保護 API 端點，驗證 Token 是否正確
  - 內容：建立一個使用 `ValidateAntiForgeryToken` 的 Controller Action

- [ ] **步驟 5: 建立前端測試頁面**
  - 原因：需要一個 HTML 頁面來測試 CSRF 防護是否有效
  - 內容：建立 HTML 頁面，包含取得 Token 和呼叫受保護 API 的功能

- [ ] **步驟 6: 設定 CORS (如需要)**
  - 原因：如果前端與後端在不同網域，需要設定 CORS
  - 內容：在 `Program.cs` 中設定 CORS 政策

- [ ] **步驟 7: 測試驗證**
  - 原因：確認 CSRF 防護機制正常運作
  - 內容：
    - 測試正常流程：取得 Token → 呼叫 API (成功)
    - 測試異常流程：不帶 Token 呼叫 API (失敗)
    - 測試異常流程：使用錯誤 Token 呼叫 API (失敗)

## 技術要點
- 使用 ASP.NET Core 內建的 `IAntiforgery` 服務
- Token 會透過 Cookie 和 Header 雙重驗證
- 支援 AJAX 請求的 CSRF 防護
- 符合 OWASP 安全建議
