# 🔒 CSRF 防護機制安全性測試計畫

**專案名稱**: Lab.CSRF.WebApi  
**測試目標**: 確保公開的 Web API 端點僅供當前頁面呼叫，防止跨站請求偽造 (CSRF) 攻擊，降低爬蟲濫用風險  
**測試日期**: 2026-01-09  
**測試人員**: 資深資安專家  
**文件版本**: 1.0

---

## 受測條件
- Web API 不需要驗證，匿名使用

## 📋 測試目標
 
### 主要目標
- [ ] 驗證 CSRF Token 機制是否正確運作
- [ ] 確認跨站請求是否被有效阻擋
- [ ] 檢測自動化工具（爬蟲）濫用風險
- [ ] 評估整體安全性防護等級

### 次要目標
- [ ] 識別潛在的安全漏洞
- [ ] 提供具體的改善建議
- [ ] 建立安全基準線

---

## 🎯 測試範圍

### 測試元件
- [ ] **前端頁面**: `/wwwroot/index.html`
- [ ] **API 端點**:
  - `GET /api/csrf/token` - Token 產生端點
  - `POST /api/csrf/protected` - 受保護的 API 端點(被測目標端點)
- [ ] **後端配置**: `Program.cs` - Anti-Forgery 與 CORS 設定
- [ ] **控制器**: `CsrfController.cs` - API 實作

### 測試類型
- [ ] 功能性測試（正常流程）
- [ ] 安全性測試（異常流程）
- [ ] 滲透測試（攻擊模擬）
- [ ] 配置審查（靜態分析）

---

## 📝 測試項目清單

### 類別 1: CSRF Token 基本功能測試

#### ✅ 測試項目 1.1: Token 產生功能
- [ ] 呼叫 `GET /api/csrf/token` 能成功取得回應
- [ ] Cookie 中正確設定 `XSRF-TOKEN`
- [ ] Token 值為非空值且符合格式
- [ ] 每次請求產生的 Token 皆不相同

**測試方法**: 使用瀏覽器開發者工具檢查 Cookie，並使用 curl 驗證

---

#### ✅ 測試項目 1.2: Token 驗證功能（正常流程）
- [ ] 攜帶正確 Token 的請求能成功通過驗證
- [ ] 伺服器正確回應成功訊息（HTTP 200 OK）
- [ ] 回應內容包含預期的資料格式與內容

**測試方法**: 使用前端頁面「使用 Token 呼叫 API」按鈕

---

#### ✅ 測試項目 1.3: Token 驗證功能（異常流程）
- [ ] 不攜帶 Token 的請求被拒絕（HTTP 400 Bad Request）
- [ ] 攜帶錯誤 Token 的請求被拒絕（HTTP 400 Bad Request）
- [ ] 使用過期 Token 的請求被拒絕（HTTP 400 Bad Request）

**測試方法**: 使用前端頁面「不使用 Token 呼叫 API」按鈕，使用 curl 測試錯誤 Token

---

### 類別 2: 跨站請求防護測試

#### ✅ 測試項目 2.1: 跨站請求阻擋（瀏覽器場景）
- [ ] 從外部網站發起的請求無法取得 Token
- [ ] 從外部網站發起的請求被 SameSite Cookie 阻擋
- [ ] CORS 政策正確運作
- [ ] 測試不同瀏覽器對 SameSite Cookie 的行為差異（Chrome、Edge、Firefox、Safari）
- [ ] 驗證舊版瀏覽器的相容性處理

**測試方法**: 建立外部 HTML 測試頁面，模擬跨站攻擊，在多種瀏覽器環境測試

---

#### ✅ 測試項目 2.2: Cookie 安全性配置
- [ ] Cookie 設定了 `SameSite=Strict` 屬性
- [ ] Cookie 的 `HttpOnly` 設定符合需求（應為 `false`，以供 JavaScript 讀取）
- [ ] HTTPS 環境下 `Secure` 旗標正確設定為 `true`

**測試方法**: 檢查程式碼配置，並使用開發者工具驗證 Cookie 屬性

---

#### ✅ 測試項目 2.3: CORS 政策檢查
- [ ] 記錄當前 CORS 設定（AllowAll 政策）
- [ ] 評估 CORS 寬鬆設定的安全風險
- [ ] 測試跨域請求是否允許

**測試方法**: 靜態程式碼審查，使用不同來源測試

---

### 類別 3: 自動化工具（爬蟲）防護測試

#### ⚠️ 測試項目 3.1: 命令列工具測試（curl）

**核心測試問題**: 使用 curl 呼叫 Web API 是否能繞過 CSRF 防護？

**測試背景說明**:
- curl 不是瀏覽器,不受 CORS 政策限制
- curl 可任意設定 Origin、User-Agent 等 Header
- SameSite Cookie 是瀏覽器行為,curl 不遵守
- 需驗證伺服器端防護機制是否對 curl 有效

**預期結果分析**:
- 可能性 A: **被阻擋** ✅ (理想狀態) - Origin/User-Agent 驗證有效
- 可能性 B: **可繞過** ⚠️ (安全漏洞) - curl 可模擬所有必要的 Header 和 Cookie

---

##### 子測試 3.1.1: curl 基本 CSRF Token 流程測試

**步驟 3.1.1.1: 取得 CSRF Token (不使用 Cookie 管理)**
```bash
curl -X GET http://localhost:5073/api/csrf/token -v
```
- [ ] 檢查 HTTP 狀態碼 (預期 400 或 403 - 缺少 Origin 或 User-Agent 驗證失敗)
- [ ] 檢查錯誤訊息內容
- [ ] 確認是否有 Set-Cookie Header

**步驟 3.1.1.2: 取得 CSRF Token (使用 Cookie Jar)**
```bash
curl -X GET http://localhost:5073/api/csrf/token -c cookies.txt -v
```
- [ ] 檢查是否產生 cookies.txt 檔案
- [ ] 檢查 cookies.txt 內容是否包含 XSRF-TOKEN
- [ ] 確認 HTTP 回應狀態

**步驟 3.1.1.3: 模擬瀏覽器 User-Agent 取得 Token**
```bash
curl -X GET http://localhost:5073/api/csrf/token \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -c cookies.txt -v
```
- [ ] User-Agent 偽造是否有效
- [ ] Origin 驗證是否生效
- [ ] 記錄實際行為

**步驟 3.1.1.4: 同時偽造 User-Agent 和 Origin**
```bash
curl -X GET http://localhost:5073/api/csrf/token \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://localhost:5073" \
  -c cookies.txt -v
```
- [ ] 是否成功取得 Token
- [ ] cookies.txt 是否包含有效的 XSRF-TOKEN
- [ ] JSON 回應是否包含 Nonce
- [ ] **關鍵測試點** - 決定防護是否有效

---

##### 子測試 3.1.2: curl 呼叫受保護的 API

**前提**: 假設步驟 3.1.1.4 成功取得 Token 和 Nonce

**步驟 3.1.2.1: 使用取得的 Token 和 Nonce 呼叫 API**
```bash
# 從 cookies.txt 讀取 Token
TOKEN=$(grep XSRF-TOKEN cookies.txt | awk '{print $7}')
NONCE="<從步驟 3.1.1.4 取得的 nonce 值>"

curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://localhost:5073" \
  -H "X-CSRF-TOKEN: $TOKEN" \
  -H "X-Nonce: $NONCE" \
  -b cookies.txt \
  -d '{"data":"curl test"}' \
  -v
```
- [ ] 檢查 HTTP 狀態碼
- [ ] 檢查 CORS 錯誤訊息
- [ ] 確認 API 是否實際執行
- [ ] **關鍵風險評估點**

**步驟 3.1.2.2: 不攜帶 Cookie 但有 Header Token (測試 Double Submit)**
```bash
curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://localhost:5073" \
  -H "X-CSRF-TOKEN: $TOKEN" \
  -H "X-Nonce: $NONCE" \
  -d '{"data":"curl test"}' \
  -v
```
- [ ] 驗證 Double Submit Cookie 機制是否有效 (預期失敗)
- [ ] 確認必須同時有 Cookie 和 Header

**步驟 3.1.2.3: 攜帶 Cookie 但不帶 Header Token**
```bash
curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://localhost:5073" \
  -H "X-Nonce: $NONCE" \
  -b cookies.txt \
  -d '{"data":"curl test"}' \
  -v
```
- [ ] HTTP 400 Bad Request (預期失敗)
- [ ] 錯誤訊息明確指出缺少 CSRF Token

**步驟 3.1.2.4: Token 重放攻擊測試**
```bash
# 使用已消費的 Nonce 再次呼叫
curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://localhost:5073" \
  -H "X-CSRF-TOKEN: $TOKEN" \
  -H "X-Nonce: $NONCE" \
  -b cookies.txt \
  -d '{"data":"replay attack"}' \
  -v
```
- [ ] HTTP 400 Bad Request (預期失敗)
- [ ] 錯誤訊息: "Nonce 無效或已使用（防止重放攻擊）"
- [ ] 驗證 Nonce 一次性使用機制

---

##### 子測試 3.1.3: 偽造與繞過測試

**步驟 3.1.3.1: 完全偽造請求 (無任何安全 Header)**
```bash
curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -d '{"data":"attack"}' \
  -v
```
- [ ] 哪一層防護最先阻擋 (Origin、User-Agent、CSRF Token)

**步驟 3.1.3.2: 偽造錯誤的 Token**
```bash
curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://localhost:5073" \
  -H "X-CSRF-TOKEN: fake-token-12345" \
  -H "X-Nonce: fake-nonce-67890" \
  -b cookies.txt \
  -d '{"data":"fake token"}' \
  -v
```
- [ ] CSRF Token 驗證機制是否有效 (預期失敗)
- [ ] 錯誤訊息是否明確

**步驟 3.1.3.3: 測試跨域請求 (偽造外部來源)**
```bash
curl -X POST http://localhost:5073/api/csrf/protected \
  -H "Content-Type: application/json" \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
  -H "Origin: http://evil.com" \
  -H "X-CSRF-TOKEN: $TOKEN" \
  -H "X-Nonce: $NONCE" \
  -b cookies.txt \
  -d '{"data":"cross origin"}' \
  -v
```
- [ ] Origin 驗證是否有效 (預期失敗)
- [ ] CORS 政策是否正確執行

---

##### 子測試 3.1.4: curl 速率限制測試

**步驟 3.1.4.1: 短時間大量請求**
```bash
# 執行 10 次連續請求
for i in {1..10}; do
  curl -X GET http://localhost:5073/api/csrf/token \
    -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" \
    -H "Origin: http://localhost:5073" \
    -v 2>&1 | grep "HTTP"
  sleep 0.1
done
```
- [ ] 記錄第幾次請求開始被限制 (預期觸發 429 Too Many Requests)
- [ ] 確認 Rate Limiting 配置是否有效
- [ ] 檢查限制解除時間

---

**測試方法**: 使用 curl 模擬完整請求流程,包含 Cookie 會話管理與多種攻擊場景

---

#### ⚠️ 測試項目 3.2: Python 爬蟲測試
- [ ] Python requests 能否取得 Token
- [ ] Python requests 能否使用 Token 呼叫受保護 API
- [ ] 測試 requests.Session() 的會話管理能力
- [ ] 驗證 Cookie 持久化與重用機制
- [ ] 評估防護效果

**測試方法**: 編寫並執行 Python 測試腳本，包含會話管理測試

---

#### ⚠️ 測試項目 3.3: Postman 測試
- [ ] Postman 能否取得 Token
- [ ] Postman 能否使用 Token 呼叫受保護 API
- [ ] Postman 自動會話管理是否有效
- [ ] 測試手動設定 Cookie 的行為
- [ ] 評估防護效果

**測試方法**: 使用 Postman 手動測試，包含會話與 Cookie 管理驗證

---

### 類別 4: 進階安全性測試

#### 🔴 測試項目 4.1: 速率限制（Rate Limiting）
- [ ] 檢查是否有速率限制機制
- [ ] 短時間大量請求是否被阻擋
- [ ] 定義正常使用者的請求頻率基準線（例如：每分鐘 60 次）
- [ ] 定義惡意使用者的阻擋閾值（例如：每分鐘 100 次）
- [ ] 測試達到閾值後的回應行為（429 Too Many Requests）
- [ ] 評估 DDoS 防護能力

**測試方法**: 使用腳本發送不同頻率的並發請求，驗證閾值設定

---

#### 🔴 測試項目 4.2: User-Agent 驗證
- [ ] 檢查是否驗證 User-Agent Header
- [ ] 已知爬蟲工具的 User-Agent 是否被阻擋
- [ ] 評估爬蟲防護能力

**測試方法**: 使用不同 User-Agent 發送請求

---

#### 🔴 測試項目 4.3: Referer/Origin 驗證
- [ ] 檢查是否驗證 Referer Header
- [ ] 檢查是否驗證 Origin Header
- [ ] 測試 Origin 與 Referer 的差異性（Origin 更可靠，無法被輕易偽造）
- [ ] 來自非法來源的請求是否被阻擋
- [ ] 測試缺少 Referer/Origin Header 的請求處理
- [ ] 測試偽造 Referer 的防護效果
- [ ] 評估來源驗證效果

**測試方法**: 使用不同 Referer 與 Origin 發送請求，比對驗證結果

---

#### 🟡 測試項目 4.4: Token 時效性
- [ ] 檢查 Token 的有效期限設定
- [ ] Token 是否會適時過期
- [ ] 過期處理是否正確

**測試方法**: 檢查程式碼配置，等待時間測試

---

#### 🟡 測試項目 4.5: 日誌與監控
- [ ] 檢查是否記錄安全相關事件
- [ ] 檢查是否記錄失敗的請求
- [ ] 評估可追蹤性

**測試方法**: 檢查程式碼，查看日誌輸出

---

#### 🔴 測試項目 4.6: Token 重放攻擊（Replay Attack）
- [ ] 擷取已使用的 Token
- [ ] 嘗試重複使用相同 Token 發送多次請求
- [ ] 驗證系統是否阻擋 Token 重放
- [ ] 檢查 Token 是否為一次性使用（Single-Use）
- [ ] 評估重放攻擊的風險等級

**測試方法**: 攔截並儲存已使用的 Token，嘗試重複發送請求

---

#### 🟡 測試項目 4.7: Token 熵值（Entropy）分析
- [ ] 收集多組 Token 樣本（建議 100+ 組）
- [ ] 分析 Token 的隨機性與不可預測性
- [ ] 檢查 Token 是否包含可識別的模式或序列
- [ ] 評估 Token 生成演算法的強度
- [ ] 驗證 Token 長度是否符合安全標準（建議 128+ bits）

**測試方法**: 統計分析多組 Token，使用熵值計算工具評估隨機性

---

#### 🟡 測試項目 4.8: HTTPS 降級攻擊測試
- [ ] 測試在 HTTP 環境下的 Cookie 行為
- [ ] 驗證 Secure 旗標是否正確阻擋 HTTP 傳輸
- [ ] 測試中間人攻擊（MITM）情境下的防護
- [ ] 檢查是否強制 HTTPS 重導向
- [ ] 評估傳輸層安全性

**測試方法**: 在 HTTP 與 HTTPS 環境分別測試，模擬降級攻擊

---

#### 🟢 測試項目 4.9: 瀏覽器相容性測試
- [ ] Chrome（最新版）的 SameSite Cookie 行為
- [ ] Edge（最新版）的 SameSite Cookie 行為
- [ ] Firefox（最新版）的 SameSite Cookie 行為
- [ ] Safari（最新版）的 SameSite Cookie 行為
- [ ] 舊版瀏覽器的相容性（IE11、舊版 Safari）
- [ ] 行動裝置瀏覽器（iOS Safari、Android Chrome）
- [ ] 評估跨瀏覽器一致性

**測試方法**: 在不同瀏覽器與版本執行相同測試案例，比對結果

---

### 類別 5: 配置安全性審查

#### ✅ 測試項目 5.1: Anti-Forgery 配置審查
- [ ] HeaderName 設定正確
- [ ] Cookie 名稱設定正確
- [ ] SameSite 設定符合安全要求
- [ ] SecurePolicy 設定符合環境需求

**測試方法**: 靜態程式碼審查

---

#### ⚠️ 測試項目 5.2: CORS 配置審查
- [ ] 評估 `AllowAnyOrigin` 的安全風險
- [ ] 評估 `AllowAnyMethod` 的安全風險
- [ ] 評估 `AllowAnyHeader` 的安全風險
- [ ] 提供針對性的改善建議

**測試方法**: 靜態程式碼審查，並比對安全最佳實踐

---

#### ✅ 測試項目 5.3: 控制器實作審查
- [ ] `[IgnoreAntiforgeryToken]` 使用是否合理
- [ ] `[ValidateAntiForgeryToken]` 是否正確套用
- [ ] API 端點是否具備適當的安全驗證機制

**測試方法**: 靜態程式碼審查

---

## 🛠️ 測試工具

### 手動測試工具
- [ ] 瀏覽器（Chrome/Edge）開發者工具
- [ ] Postman / Insomnia
- [ ] curl 命令列工具

### 自動化測試工具
- [ ] Python requests 套件
- [ ] Bash 腳本（並發測試）
- [ ] 自訂測試腳本

### 分析工具
- [ ] 靜態程式碼分析（人工審查）
- [ ] 網路封包分析（如需要）

---

## 📊 測試標準

### 功能性測試
- **通過標準**: 正常流程 100% 成功
- **失敗標準**: 異常流程 100% 被阻擋

### 安全性測試
- **高安全**: 傳統 CSRF 攻擊 100% 阻擋
- **中安全**: 爬蟲濫用 > 80% 阻擋
- **低安全**: 爬蟲濫用 < 50% 阻擋

### 風險等級定義
- 🔴 **嚴重**: 可直接造成資料洩漏或服務癱瘓
- 🟠 **高風險**: 顯著降低安全性，容易被利用
- 🟡 **中風險**: 有安全隱患，但利用難度較高
- 🟢 **低風險**: 理論風險，實際影響小

---

## 📅 測試執行計畫

### 階段 1: 基礎功能驗證（30 分鐘）
- [ ] 測試項目 1.1 - Token 產生功能
- [ ] 測試項目 1.2 - Token 驗證（正常流程）
- [ ] 測試項目 1.3 - Token 驗證（異常流程）

### 階段 2: CSRF 防護驗證（30 分鐘）
- [ ] 測試項目 2.1 - 跨站請求阻擋
- [ ] 測試項目 2.2 - Cookie 安全性
- [ ] 測試項目 2.3 - CORS 政策

### 階段 3: 爬蟲防護測試（45 分鐘）
- [ ] 測試項目 3.1 - curl 測試
- [ ] 測試項目 3.2 - Python 爬蟲測試
- [ ] 測試項目 3.3 - Postman 測試

### 階段 4: 進階安全測試（90 分鐘）
- [ ] 測試項目 4.1 - 速率限制
- [ ] 測試項目 4.2 - User-Agent 驗證
- [ ] 測試項目 4.3 - Referer/Origin 驗證
- [ ] 測試項目 4.4 - Token 時效性
- [ ] 測試項目 4.5 - 日誌監控
- [ ] 測試項目 4.6 - Token 重放攻擊
- [ ] 測試項目 4.7 - Token 熵值分析
- [ ] 測試項目 4.8 - HTTPS 降級攻擊
- [ ] 測試項目 4.9 - 瀏覽器相容性

### 階段 5: 配置審查（30 分鐘）
- [ ] 測試項目 5.1 - Anti-Forgery 配置
- [ ] 測試項目 5.2 - CORS 配置
- [ ] 測試項目 5.3 - Controller 實作

### 階段 6: 報告撰寫（30 分鐘）
- [ ] 彙整測試結果
- [ ] 分析安全風險
- [ ] 提供改善建議
- [ ] 產生測試報告

**預計總時間**: 4.0 小時

---

## 📋 測試資料準備

### 測試 API 端點
- **Token 端點**: `http://localhost:5073/api/csrf/token`
- **Protected 端點**: `http://localhost:5073/api/csrf/protected`

### 測試資料
```json
{
  "data": "Security Test Data"
}
```

### 測試場景
1. **正常使用者**: 從前端頁面操作
2. **惡意使用者**: 從外部網站攻擊
3. **自動化工具**: 使用爬蟲嘗試存取
4. **DDoS 攻擊**: 大量並發請求

---

## ✅ 完成標準

### 測試計畫完成條件
- [ ] 所有測試項目都已列出
- [ ] 每個測試項目都有明確的驗證標準
- [ ] 測試方法清楚說明
- [ ] 測試工具準備就緒

### 測試執行完成條件
- [ ] 所有測試項目都已執行
- [ ] 每個測試項目都有記錄結果
- [ ] 所有發現的問題都已記錄
- [ ] 測試報告已產生

---

## 📝 備註

### 安全性考量
- 測試過程中避免實際破壞資料或影響系統穩定性
- 僅在開發環境執行測試，避免影響生產環境
- 測試腳本執行後須清理產生的測試資料

### 預期風險
根據初步分析，預期可能發現以下安全問題：
1. 🔴 爬蟲可完全繞過 CSRF 防護（嚴重）
2. 🔴 缺乏速率限制機制（嚴重）
3. 🔴 Token 可能被重放攻擊利用（嚴重）
4. 🟠 CORS 政策過於寬鬆（高風險）
5. 🟠 缺少 Referer/Origin 驗證（高風險）
6. 🟠 缺少 User-Agent 驗證（高風險）
7. 🟡 Token 熵值可能不足（中風險）
8. 🟡 HTTPS 降級攻擊風險（中風險）
9. 🟢 瀏覽器相容性問題（低風險）

### 改善建議方向
1. 實作速率限制（Rate Limiting）機制
2. 加入 Referer/Origin Header 驗證
3. 加入 User-Agent 驗證與過濾
4. 收緊 CORS 政策，限制允許的來源
5. 加入日誌監控與異常偵測機制
6. 實作 Token 一次性使用（Single-Use）機制
7. 強化 Token 生成演算法，提升熵值
8. 強制 HTTPS 連線，防止降級攻擊
9. 針對不同瀏覽器進行相容性處理

---

**測試計畫制定日期**: 2026-01-09  
**計畫制定人**: 資深資安專家  
**審查狀態**: ✅ 已完成

---

## 🚀 下一步行動

1. **執行測試**: 按照本計畫逐項執行測試，並記錄結果至 `security-test-result.md`
2. **分析結果**: 分析測試結果，識別安全漏洞與風險等級
3. **制定改善方案**: 根據測試結果，產生 `security-test-improve.plan.md` 改善計畫