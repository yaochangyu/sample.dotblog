# CSRF 安全改善實作計畫

## 目標
將當前的 CSRF Token 機制升級為「Token 與 Session 綁定」的安全模式，防止攻擊者直接取得 Token 並發動攻擊。

## 改善方案：方案 1（Token 與 Session 綁定）

### 核心概念
- Token 必須與使用者的 Session ID 綁定
- 即使攻擊者取得 Token，沒有對應的 Session Cookie 也無法使用
- Session Cookie 設定 HttpOnly、Secure、SameSite 防止竊取

---

## 實作步驟

### ✅ 步驟 1：更新 Program.cs - 啟用 Session 與相關服務

**為什麼需要這個步驟？**
- 當前專案沒有啟用 Session 功能
- 需要註冊 IHttpContextAccessor 才能在 Service 中存取 HttpContext
- 需要將 MemoryCache 改為 IDistributedCache（支援分散式快取）
- Session Cookie 需要設定安全屬性（HttpOnly, Secure, SameSite）

**需要做什麼？**
1. 註冊 `AddHttpContextAccessor()`
2. 註冊 `AddDistributedMemoryCache()`
3. 啟用 Session 並設定安全的 Cookie 屬性
4. 在 middleware pipeline 中加入 `UseSession()`
5. 更新 CORS 設定確保允許 Credentials

**修改檔案**：`Lab.CSRF.WebApi/Program.cs`

---

### ✅ 步驟 2：修改 CsrfTokenService - 實作 Token 與 Session 綁定

**為什麼需要這個步驟？**
- 當前的 Token 只是單純儲存在 Cache 中，沒有綁定任何身份
- 攻擊者可以取得 Token 並直接使用
- 需要將 Token 與 Session ID 綁定，形成 `csrf:{sessionId}:{token}` 的 Key

**需要做什麼？**
1. 注入 IHttpContextAccessor 和 IDistributedCache
2. 在 `GenerateToken()` 中取得 Session ID
3. 使用 `csrf:{sessionId}:{token}` 作為 Cache Key
4. 在 `ValidateToken()` 中驗證 Token 是否與當前 Session 配對
5. 在 `RemoveToken()` 中使用正確的 Cache Key 刪除
6. 加入適當的錯誤處理（Session 不存在時）

**修改檔案**：`Lab.CSRF.WebApi/Services/CsrfTokenService.cs`

---

### ✅ 步驟 3：更新前端測試頁面

**為什麼需要這個步驟？**
- 前端需要在所有請求中加入 `credentials: 'include'`
- 這樣才能傳送和接收 Session Cookie
- 沒有這個設定，Session 機制將無法運作

**需要做什麼？**
1. 檢查專案中是否有前端測試頁面（HTML/JavaScript）
2. 在所有 fetch 請求中加入 `credentials: 'include'`
3. 確保 CORS 設定允許 Credentials

**修改檔案**：`wwwroot` 中的測試頁面（如果存在）

---

### ✅ 步驟 4：安全性測試

**為什麼需要這個步驟？**
- 驗證改善方案是否有效
- 確保攻擊者無法繞過新的安全機制
- 測試正常的使用情境是否運作正常

**需要做什麼？**
1. 測試正常情境：取得 Token → 使用 Token → 成功
2. 測試攻擊情境 1：直接使用 curl 取得 Token → 使用 Token → 失敗（403）
3. 測試攻擊情境 2：在不同 Session 中使用 Token → 失敗（403）
4. 測試 Token 過期情境
5. 檢查 Session Cookie 的安全屬性

**測試方式**：使用 curl 命令或瀏覽器開發者工具

---

## 安全性提升對照

| 攻擊場景 | 改善前 | 改善後 |
|---------|--------|--------|
| 攻擊者直接取得 Token | ❌ 可行 | ✅ 無效（沒有對應 Session） |
| 攻擊者竊取 Cookie | N/A | ✅ 防護（HttpOnly + Secure） |
| 跨站攻擊 | ⚠️ 部分防護 | ✅ 完全防護（SameSite） |
| 自動化攻擊 | ❌ 可行 | ✅ 防護（需要有效 Session） |

---

## 預期成果

1. ✅ Token 只能在建立它的 Session 中使用
2. ✅ 攻擊者無法透過取得 Token 就能發動攻擊
3. ✅ Session Cookie 受到完整保護（HttpOnly, Secure, SameSite）
4. ✅ 正常的前端應用程式仍可正常運作
5. ✅ 符合 OWASP CSRF 防護最佳實踐

---

## 注意事項

1. **Session 儲存**：目前使用 In-Memory Session，生產環境建議使用 Redis
2. **HTTPS**：Session 的 Secure 屬性要求必須使用 HTTPS
3. **CORS**：必須正確設定 AllowCredentials，否則 Cookie 無法傳送
4. **向後相容性**：此改動會破壞現有的 API 使用方式，需要更新前端程式碼
