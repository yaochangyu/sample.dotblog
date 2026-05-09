# Lab.PKCE — SPA 安全架構實作範例（PKCE + CSP）

> **Lab 性質教學專案**，目的是把 PKCE、CSP、Session Cookie 與 Bearer Token 的關係講清楚，不是完整的 OAuth 2.0 Authorization Server 產品。

---

## 簡介

示範 SPA（Single Page Application）如何透過：

- **Authorization Code Flow + PKCE**（RFC 7636）防止授權碼被攔截後換取 Token
- **Content Security Policy（CSP）** 從瀏覽器端限制腳本來源，降低 XSS 攻擊面

兩道防線合在一起，構成比較完整的 SPA 安全架構。

---

## 技術棧

| 項目 | 版本 |
|------|------|
| .NET | 10 |
| ASP.NET Core Web API | 10 |
| 前端 | 原生 HTML / JavaScript（Web Crypto API） |
| Mermaid.js | CDN（cdn.jsdelivr.net） |

---

## 專案結構

```text
Security/Lab.PKCE/
├── AuthServer/
│   ├── Controllers/
│   │   ├── AuthorizeController.cs   # POST /authorize、GET /authorize/session
│   │   ├── TokenController.cs       # POST /token（PKCE 驗證、核發 Token）
│   │   ├── MeController.cs          # GET /api/me（受保護端點）
│   │   └── LogoutController.cs      # POST /logout
│   ├── Models/                      # 資料模型
│   ├── Services/
│   │   ├── PkceService.cs           # SHA-256 PKCE 驗證（timing-safe）
│   │   ├── UserStore.cs             # 記憶體使用者（密碼雜湊）
│   │   ├── SessionStore.cs          # Session 管理
│   │   ├── AuthorizationCodeStore.cs# 單次使用授權碼
│   │   ├── AccessTokenStore.cs      # Token 儲存
│   │   └── OAuthClientPolicy.cs     # Client ID / Redirect URI 白名單
│   ├── wwwroot/
│   │   ├── emulator.html            # SPA PKCE + CSP 互動模擬器
│   │   ├── emulator.js              # 前端邏輯（Web Crypto API）
│   │   └── flow.html                # Mermaid 流程圖
│   └── Program.cs
├── Lab.PKCE.sln
└── SPA-PKCE-CSP-Security.md        # 技術文章
```

---

## 啟動方式

```bash
cd AuthServer
dotnet run --launch-profile https
```

| Profile | URL |
|---------|-----|
| https   | https://localhost:7070 |
| http    | http://localhost:5283  |

瀏覽器開啟 `https://localhost:7070/emulator.html` 即可使用互動模擬器。

---

## 測試帳號

| 帳號  | 密碼         |
|-------|-------------|
| alice | password123 |
| bob   | password456 |

---

## API 端點

| Method | 路徑                  | 說明                              |
|--------|-----------------------|-----------------------------------|
| POST   | `/authorize`          | 驗帳密或 Session，回傳 Authorization Code |
| GET    | `/authorize/session`  | 查詢目前 Session 是否有效          |
| POST   | `/token`              | 以 code + code_verifier 換取 Access Token |
| POST   | `/logout`             | 清除 Session Cookie               |
| GET    | `/api/me`             | 受保護端點，需帶 Bearer Token      |

---

## PKCE 流程

```
瀏覽器                  SPA                  授權伺服器            後端 API
  │                      │                        │                    │
  │──── 載入頁面 ────────▶│                        │                    │
  │                      │── GET /authorize/session ──▶                │
  │         [無 Session] │◀── 401 ───────────────│                    │
  │◀─── 顯示登入表單 ────│                        │                    │
  │──── 帳號密碼 ────────▶│                        │                    │
  │                      │── 產生 code_verifier ──▶（本地）            │
  │                      │── POST /authorize ─────▶                   │
  │                      │   (帳密 + code_challenge)                   │
  │                      │◀── Set-Cookie: sid ────│                    │
  │                      │◀── authorization_code ─│                    │
  │                      │── POST /token ─────────▶                   │
  │                      │   (code + code_verifier)                    │
  │                      │◀── access_token ───────│                    │
  │                      │── GET /api/me ──────────────────────────────▶
  │◀─── 顯示使用者資訊 ──│                        │◀─── 200 { username }
```

**Session 有效時**（靜默換 Token）：跳過帳密步驟，直接以 Cookie 取得 Authorization Code。

---

## 安全機制

### PKCE（S256）
- 前端以 `window.crypto.subtle` 產生 `code_verifier`（隨機 32 bytes）
- `code_challenge = BASE64URL(SHA256(code_verifier))`
- 伺服器端使用 `CryptographicOperations.FixedTimeEquals` 做 timing-safe 比對

### Session Cookie
```
HttpOnly=true; Secure=true; SameSite=Strict
```

### CSP（Content Security Policy）
伺服器預設回應標頭：

```
default-src 'self';
script-src 'self' https://cdn.jsdelivr.net;
style-src 'self' 'unsafe-inline';
img-src 'self' data:;
connect-src 'self';
object-src 'none';
base-uri 'none';
frame-ancestors 'none';
form-action 'self'
```

> `emulator.html` 預設由伺服器送出 CSP Header；切換至 `?csp=off` 重新載入即可模擬無防護狀態，比較 XSS 攻擊的行為差異。

### 其他安全標頭
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy`（停用攝影機、麥克風、地理位置等）

---

## 已知限制（Lab 性質）

- Token 格式為假 JWT（`eyJ.{guid}.SIG`），不具備簽章驗證
- 使用者、Session、Token 均存放記憶體，重啟即消失
- 未實作 client registry、scope 管理、refresh token
- `UserStore` 預設密碼為明文設定，僅供示範

---

## 相關文章

- `SPA-PKCE-CSP-Security.md` — 完整技術說明文章
- [RFC 7636 — PKCE](https://datatracker.ietf.org/doc/html/rfc7636)
- [RFC 6749 — OAuth 2.0](https://datatracker.ietf.org/doc/html/rfc6749)
- [W3C CSP Level 3](https://www.w3.org/TR/CSP3/)
