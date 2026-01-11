# API Protected 安全測試計畫

## 測試目標

確保 `api/protected` 端點的安全性，防止跨站請求偽造 (CSRF)、Token 洩漏濫用與爬蟲攻擊。

---

## 角色定位

- **API 端點名稱**: `api/protected`
- **存取權限**: 公開且可匿名使用
- **安全目標**: 
  - 僅供當前頁面呼叫
  - 防止 CSRF 攻擊
  - Token 洩漏後仍能防止濫用
  - 降低爬蟲濫用風險

---

## 測試項目分類

### 一、CSRF 防護測試 (跨站請求偽造)

#### 測試目的
驗證 API 端點僅能被合法來源呼叫，拒絕跨站偽造請求。

#### 測試案例

- [ ] **TC-CSRF-01: 無 Token 的請求**
  - **測試方法**: 直接呼叫 `POST /api/protected`，不帶任何 Token Header
  - **預期結果**: 回傳 `401 Unauthorized` 或 `403 Forbidden`
  - **驗證重點**: API 必須拒絕無 Token 請求

- [ ] **TC-CSRF-02: 偽造 Token 的請求**
  - **測試方法**: 呼叫 `POST /api/protected`，帶自行產生的假 Token
  - **預期結果**: 回傳 `401 Unauthorized` 或 `403 Forbidden`
  - **驗證重點**: 伺服器能識別並拒絕無效 Token

- [ ] **TC-CSRF-03: 過期 Token 的請求**
  - **測試方法**: 
    1. 取得有效 Token
    2. 等待 Token 過期（超過 10 分鐘）
    3. 使用過期 Token 呼叫 API
  - **預期結果**: 回傳 `401 Unauthorized` 或 `403 Forbidden`
  - **驗證重點**: Token 時效性管控

- [ ] **TC-CSRF-04: 重複使用 Token (超過次數限制)**
  - **測試方法**:
    1. 取得有效 Token
    2. 重複使用相同 Token 呼叫 API（超過允許次數）
  - **預期結果**: 
    - 前 N 次成功 (`200 OK`)
    - 第 N+1 次失敗 (`401 Unauthorized` 或 `403 Forbidden`)
  - **驗證重點**: Token 使用次數限制生效

- [ ] **TC-CSRF-05: 跨域請求驗證 (CORS)**
  - **測試方法**: 從不同網域的網頁發起請求
  - **預期結果**: 
    - 若未設定 CORS，瀏覽器應阻擋請求
    - 若設定 CORS，僅允許白名單網域
  - **驗證重點**: CORS 政策正確設定

- [ ] **TC-CSRF-06: Referer Header 驗證**
  - **測試方法**: 
    1. 正常請求帶正確 Referer
    2. 偽造請求帶錯誤或空 Referer
  - **預期結果**: 
    - 正確 Referer → `200 OK`
    - 錯誤/空 Referer → `403 Forbidden`
  - **驗證重點**: Referer 檢查機制（可選）

- [ ] **TC-CSRF-07: Origin Header 驗證**
  - **測試方法**:
    1. 正常請求帶正確 Origin
    2. 偽造請求帶錯誤 Origin
  - **預期結果**:
    - 正確 Origin → `200 OK`
    - 錯誤 Origin → `403 Forbidden`
  - **驗證重點**: Origin 檢查機制

---

### 二、Token 洩漏防護測試

#### 測試目的
即使 Token 被洩漏，仍能透過其他機制防止濫用。

#### 測試案例

- [ ] **TC-LEAK-01: cURL 直接使用洩漏的 Token**
  - **測試方法**:
    1. 從瀏覽器正常取得 Token
    2. 使用 cURL 攜帶該 Token 發送請求
  - **預期結果**: 
    - 若有 User-Agent 檢查 → `403 Forbidden`
    - 若有 Referer/Origin 檢查 → `403 Forbidden`
    - 若僅依賴 Token → `200 OK`（有風險）
  - **驗證重點**: Token 單獨驗證不足，需搭配其他檢查

- [ ] **TC-LEAK-02: Token 攔截後批次請求**
  - **測試方法**:
    1. 取得有效 Token
    2. 在短時間內發送大量請求（使用腳本）
  - **預期結果**:
    - 觸發速率限制 → `429 Too Many Requests`
    - 或因次數限制快速耗盡 Token → `401 Unauthorized`
  - **驗證重點**: 速率限制 (Rate Limiting) 機制

- [ ] **TC-LEAK-03: Token 在不同 IP 使用**
  - **測試方法**:
    1. 在 IP_A 取得 Token
    2. 在 IP_B 使用該 Token
  - **預期結果**:
    - 若有 IP 綁定 → `403 Forbidden`
    - 若無 IP 綁定 → `200 OK`（較低安全性）
  - **驗證重點**: IP 綁定機制（可選）

- [ ] **TC-LEAK-04: Token 在不同 User-Agent 使用**
  - **測試方法**:
    1. 使用瀏覽器取得 Token
    2. 使用 cURL (不同 User-Agent) 呼叫 API
  - **預期結果**:
    - 若有 User-Agent 驗證 → `403 Forbidden`
    - 若無驗證 → `200 OK`
  - **驗證重點**: User-Agent 一致性檢查

---

### 三、爬蟲防護測試

#### 測試目的
降低自動化工具、爬蟲程式濫用 API 的風險。

#### 測試案例

- [ ] **TC-BOT-01: 無 User-Agent 的請求**
  - **測試方法**: 發送請求時移除 User-Agent Header
  - **預期結果**: `403 Forbidden`
  - **驗證重點**: 拒絕無 User-Agent 的請求

- [ ] **TC-BOT-02: 可疑 User-Agent 的請求**
  - **測試方法**: 使用已知爬蟲 User-Agent (例如: `curl/7.68.0`, `python-requests/2.28.0`)
  - **預期結果**: `403 Forbidden`
  - **驗證重點**: User-Agent 黑名單機制

- [ ] **TC-BOT-03: 高頻率請求 (速率限制)**
  - **測試方法**: 在短時間內發送大量請求（例如：1 秒內 100 次）
  - **預期結果**: 
    - 前 N 次正常回應
    - 超過閾值後回傳 `429 Too Many Requests`
  - **驗證重點**: 速率限制生效

- [ ] **TC-BOT-04: Token 生成頻率限制**
  - **測試方法**: 頻繁請求 `GET /api/token` (例如：1 秒內 50 次)
  - **預期結果**: 
    - 前 N 次正常回傳 Token
    - 超過閾值後回傳 `429 Too Many Requests`
  - **驗證重點**: Token 生成不應被濫用

- [ ] **TC-BOT-05: JavaScript 挑戰 (可選)**
  - **測試方法**: 
    1. 純 cURL 請求 (無法執行 JavaScript)
    2. 真實瀏覽器請求 (可執行 JavaScript)
  - **預期結果**:
    - cURL → 需額外驗證才能取得 Token
    - 瀏覽器 → 正常取得 Token
  - **驗證重點**: JavaScript 驗證機制 (Captcha / 計算挑戰)

- [ ] **TC-BOT-06: Honeypot 陷阱欄位**
  - **測試方法**:
    1. 在請求中填入隱藏欄位 (正常用戶不會填寫)
    2. 正常請求不帶隱藏欄位
  - **預期結果**:
    - 帶隱藏欄位 → `403 Forbidden`
    - 不帶隱藏欄位 → `200 OK`
  - **驗證重點**: Honeypot 機制識別爬蟲

---

### 四、cURL 驗證測試 (綜合)

#### 測試目的
使用 cURL 模擬各種攻擊場景，驗證防護機制完整性。

#### 測試案例

- [ ] **TC-CURL-01: 基本 cURL 請求 (無防護)**
  - **測試指令**:
    ```bash
    curl -X POST https://localhost:5001/api/protected \
         -H "Content-Type: application/json" \
         -d '{"data":"test"}'
    ```
  - **預期結果**: `401 Unauthorized` (無 Token)

- [ ] **TC-CURL-02: cURL 帶有效 Token**
  - **測試指令**:
    ```bash
    # 步驟 1: 取得 Token
    TOKEN=$(curl -s -X GET https://localhost:5001/api/token \
                 -H "User-Agent: Mozilla/5.0" \
                 -I | grep -i "X-CSRF-Token" | cut -d' ' -f2 | tr -d '\r')
    
    # 步驟 2: 使用 Token
    curl -X POST https://localhost:5001/api/protected \
         -H "X-CSRF-Token: $TOKEN" \
         -H "Content-Type: application/json" \
         -d '{"data":"test"}'
    ```
  - **預期結果**: 
    - 若僅驗證 Token → `200 OK` ⚠️ (有風險)
    - 若檢查 Referer/Origin → `403 Forbidden` ✅

- [ ] **TC-CURL-03: cURL 偽造 Referer Header**
  - **測試指令**:
    ```bash
    curl -X POST https://localhost:5001/api/protected \
         -H "X-CSRF-Token: $TOKEN" \
         -H "Referer: https://localhost:5001/" \
         -H "Content-Type: application/json" \
         -d '{"data":"test"}'
    ```
  - **預期結果**: 
    - 若僅檢查 Referer → `200 OK` (Referer 可偽造)
    - 應搭配其他機制 (如 SameSite Cookie)

- [ ] **TC-CURL-04: cURL 偽造 Origin Header**
  - **測試指令**:
    ```bash
    curl -X POST https://localhost:5001/api/protected \
         -H "X-CSRF-Token: $TOKEN" \
         -H "Origin: https://localhost:5001" \
         -H "Content-Type: application/json" \
         -d '{"data":"test"}'
    ```
  - **預期結果**: 類似 TC-CURL-03

- [ ] **TC-CURL-05: cURL 批次攻擊測試**
  - **測試指令**:
    ```bash
    # 發送 100 次請求
    for i in {1..100}; do
      curl -X POST https://localhost:5001/api/protected \
           -H "X-CSRF-Token: $TOKEN" \
           -H "Content-Type: application/json" \
           -d '{"data":"test"}' &
    done
    wait
    ```
  - **預期結果**: 觸發速率限制 `429 Too Many Requests`

- [ ] **TC-CURL-06: cURL 修改 User-Agent**
  - **測試指令**:
    ```bash
    curl -X POST https://localhost:5001/api/protected \
         -H "X-CSRF-Token: $TOKEN" \
         -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)" \
         -H "Content-Type: application/json" \
         -d '{"data":"test"}'
    ```
  - **預期結果**: 
    - 若有 User-Agent 一致性檢查 → `403 Forbidden`
    - 若無檢查 → `200 OK`

---

## 測試執行策略

### 自動化測試腳本
建議使用 PowerShell 或 Bash 腳本執行所有測試案例，產生測試報告。

**範例結構**:
```powershell
# api-protected-security-test.ps1
# 執行所有測試案例並產生報告
```

### 前端自動化測試
針對瀏覽器互動場景，使用 Playwright 進行自動化測試。

**詳見**: [Frontend Playwright 自動化測試計畫](./frontend-playwright-test-plan.md)

---

## 建議防護機制優先順序

### 🔴 必須實作 (高優先級)
1. ✅ Token 驗證 (已實作)
2. ✅ Token 過期機制 (已實作)
3. ✅ Token 使用次數限制 (已實作)
4. ⚠️ CORS 政策設定
5. ⚠️ 速率限制 (Rate Limiting)

### 🟡 建議實作 (中優先級)
6. ⚠️ Referer / Origin Header 驗證
7. ⚠️ User-Agent 基本檢查
8. ⚠️ IP 地址綁定 (可選)

### 🟢 進階實作 (低優先級)
9. ❌ JavaScript 挑戰 / Captcha
10. ❌ Honeypot 陷阱欄位
11. ❌ 機器學習行為分析

---

## 測試報告格式

每個測試案例應記錄:
- **測試編號**: TC-XXXX-XX
- **測試名稱**: 案例描述
- **執行時間**: ISO 8601 格式
- **測試結果**: ✅ PASS / ❌ FAIL
- **實際回應**: HTTP Status Code + Response Body
- **備註**: 額外觀察或建議

---

## 風險評估

| 風險場景 | 嚴重程度 | 目前防護 | 建議改善 |
|---------|---------|---------|---------|
| CSRF 攻擊 | 🔴 高 | Token 驗證 | 新增 SameSite Cookie |
| Token 洩漏濫用 | 🟡 中 | 次數限制 | 新增 IP 綁定 + User-Agent 檢查 |
| 爬蟲批次請求 | 🟡 中 | Token 有效期 | 新增速率限制 |
| DDoS 攻擊 | 🔴 高 | 無 | 新增速率限制 + WAF |
| Referer/Origin 偽造 | 🟢 低 | CORS | CORS 已足夠 (瀏覽器強制) |

---

## 測試環境需求

- **作業系統**: Windows / Linux / macOS
- **工具**: 
  - cURL (命令列測試)
  - PowerShell 7+ 或 Bash (自動化腳本)
  - 瀏覽器 (Chrome/Edge/Firefox) 含開發者工具
- **測試伺服器**: ASP.NET Core API (https://localhost:5001)
- **網路**: 本機測試 (localhost) 與跨域測試環境

---

## 測試檢查清單總覽

### CSRF 防護測試: 7 項
- [ ] TC-CSRF-01 ~ TC-CSRF-07

### Token 洩漏防護測試: 4 項
- [ ] TC-LEAK-01 ~ TC-LEAK-04

### 爬蟲防護測試: 6 項
- [ ] TC-BOT-01 ~ TC-BOT-06

### cURL 驗證測試: 6 項
- [ ] TC-CURL-01 ~ TC-CURL-06

**總計**: 23 項測試案例

---

## 參考文件

### 相關測試計畫
- [Frontend Playwright 自動化測試計畫](./frontend-playwright-test-plan.md) - 瀏覽器環境整合測試

### 安全標準
- [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [MDN - CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [RFC 6750 - Bearer Token Usage](https://datatracker.ietf.org/doc/html/rfc6750)

---

## 版本紀錄

| 版本 | 日期 | 變更內容 | 作者 |
|------|------|---------|------|
| 1.0 | 2026-01-12 | 初版建立 | Security Team |

