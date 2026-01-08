# CSRF 防護改善方案 - 進度追蹤

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
