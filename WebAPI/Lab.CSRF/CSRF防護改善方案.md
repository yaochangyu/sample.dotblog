# CSRF 防護改善方案

## 問題說明
目前的實作允許任何人透過 `/api/csrf/token` 端點取得 Token，這使得：
- 爬蟲可以輕易取得 Token
- 自動化攻擊可以先取 Token 再呼叫 API
- 無法真正防止 CSRF 攻擊

## 改善目標
採用 **Double Submit Cookie 模式**，確保：
- Token 透過 Cookie 自動傳送（無法跨站讀取）
- 前端必須從 Cookie 讀取 Token 並放入 Header
- 跨站請求無法讀取 Cookie，因此無法偽造請求

## 實作步驟

- [ ] **步驟 1: 移除 Token 端點的 JSON 回傳**
  - 原因：Token 不應透過 JSON 暴露給前端，應僅存在 Cookie 中
  - 內容：修改 `/api/csrf/token` 端點，不回傳 Token 值，僅設定 Cookie

- [ ] **步驟 2: 設定 Cookie 為非 HttpOnly**
  - 原因：前端需要能讀取 Cookie 中的 Token 值，才能放入 Header
  - 內容：設定 Anti-Forgery Cookie 的 `HttpOnly = false`，但保持 `SameSite = Strict`

- [ ] **步驟 3: 調整 Token Cookie 名稱**
  - 原因：明確區分 CSRF Token Cookie 和驗證用 Cookie
  - 內容：設定 Cookie 名稱為 `X-CSRF-TOKEN` 或 `XSRF-TOKEN`

- [ ] **步驟 4: 更新前端取得 Token 的方式**
  - 原因：前端不再從 JSON 讀取 Token，改從 Cookie 讀取
  - 內容：新增 JavaScript 函式從 Cookie 讀取 Token 值

- [ ] **步驟 5: 更新前端送出 Token 的方式**
  - 原因：確保 Token 同時存在於 Cookie 和 Header 中
  - 內容：從 Cookie 讀取 Token，放入 `X-CSRF-TOKEN` Header

- [ ] **步驟 6: 測試驗證**
  - 原因：確認改善後的機制能有效防止 CSRF 攻擊
  - 內容：
    - 測試正常流程：訪問頁面自動取得 Cookie → 呼叫 API (成功)
    - 測試異常流程：跨站請求無法讀取 Cookie → 呼叫 API (失敗)
    - 測試異常流程：偽造 Header 但無 Cookie → 呼叫 API (失敗)

## 技術要點
- Cookie 設定：`SameSite = Strict`, `HttpOnly = false`, `Secure = true` (HTTPS)
- Double Submit 驗證：比對 Cookie 和 Header 中的 Token 是否一致
- 跨站請求無法讀取其他網站的 Cookie，因此無法取得 Token
- 即使爬蟲訪問網站，也無法在跨站情境下使用 Token

## 安全性提升
✅ 防止跨站請求偽造 (CSRF)
✅ 防止爬蟲跨站濫用 API
✅ 符合 OWASP Double Submit Cookie 模式
✅ 不需要使用者登入即可運作
