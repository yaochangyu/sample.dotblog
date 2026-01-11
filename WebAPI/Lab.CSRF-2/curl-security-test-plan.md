# API 安全性測試計畫 - 使用 cURL

## 📋 測試目標
驗證 `api/protected` 端點的 Token 驗證機制，確保 API 受到適當保護，防止未授權存取。

## 🎯 測試範圍
測試 `/api/protected` 端點在不同情境下的安全性表現。

## 🧪 測試項目清單

### ✅ 測試項目 1：缺少 Token Header - 應拒絕存取
**目的**: 驗證 API 是否拒絕未攜帶 Token 的請求  
**預期結果**: HTTP 401 Unauthorized  
**cURL 命令**:
```bash
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -d '{"data":"測試資料"}' \
  -k -i
```
**必要性**: 確保基本的驗證機制有效，未授權請求會被拒絕

---

### ✅ 測試項目 2：使用無效/偽造的 Token - 應拒絕存取
**目的**: 驗證 API 是否拒絕無效或偽造的 Token  
**預期結果**: HTTP 401 Unauthorized  
**cURL 命令**:
```bash
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: fake-invalid-token-12345" \
  -d '{"data":"測試資料"}' \
  -k -i
```
**必要性**: 防止攻擊者使用偽造 Token 繞過驗證

---

### ✅ 測試項目 3：使用有效 Token (首次使用) - 應允許存取
**目的**: 驗證正常的 Token 使用流程  
**預期結果**: HTTP 200 OK  
**cURL 命令**:
```bash
# 步驟 1: 取得 Token
TOKEN=$(curl -X GET "https://localhost:7001/api/token?maxUsage=2&expirationMinutes=5" \
  -k -i -s | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

# 步驟 2: 使用 Token 呼叫 Protected API
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: $TOKEN" \
  -d '{"data":"測試資料第一次"}' \
  -k -i
```
**必要性**: 驗證正常使用流程能正確運作

---

### ✅ 測試項目 4：Token 重複使用 (maxUsage=2) - 第二次應允許
**目的**: 驗證 Token 使用次數計數機制  
**預期結果**: HTTP 200 OK  
**cURL 命令**:
```bash
# 使用相同 Token 第二次呼叫
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: $TOKEN" \
  -d '{"data":"測試資料第二次"}' \
  -k -i
```
**必要性**: 確保使用次數限制功能正確運作

---

### ✅ 測試項目 5：Token 超過使用次數限制 - 應拒絕存取
**目的**: 驗證超過 maxUsage 限制時會被拒絕  
**預期結果**: HTTP 401 Unauthorized  
**cURL 命令**:
```bash
# 使用相同 Token 第三次呼叫 (超過 maxUsage=2)
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: $TOKEN" \
  -d '{"data":"測試資料第三次"}' \
  -k -i
```
**必要性**: 防止 Token 被無限次重複使用，增強安全性

---

### ✅ 測試項目 6：使用過期的 Token - 應拒絕存取
**目的**: 驗證 Token 過期機制  
**預期結果**: HTTP 401 Unauthorized  
**cURL 命令**:
```bash
# 步驟 1: 取得短效 Token (1 秒過期)
EXPIRED_TOKEN=$(curl -X GET "https://localhost:7001/api/token?maxUsage=5&expirationMinutes=0.016" \
  -k -i -s | grep -i "X-CSRF-Token:" | cut -d' ' -f2 | tr -d '\r')

# 步驟 2: 等待 Token 過期
sleep 2

# 步驟 3: 使用過期 Token
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: $EXPIRED_TOKEN" \
  -d '{"data":"測試過期Token"}' \
  -k -i
```
**必要性**: 確保過期 Token 無法被使用，防止 Token 長期有效帶來的風險

---

### ✅ 測試項目 7：空白 Token Header - 應拒絕存取
**目的**: 驗證空白 Token 是否會被拒絕  
**預期結果**: HTTP 401 Unauthorized  
**cURL 命令**:
```bash
curl -X POST https://localhost:7001/api/protected \
  -H "Content-Type: application/json" \
  -H "X-CSRF-Token: " \
  -d '{"data":"測試資料"}' \
  -k -i
```
**必要性**: 防止攻擊者使用空值繞過驗證

---

## 🔧 參數說明

### cURL 參數
- `-X POST`: HTTP POST 方法
- `-H`: 設定 HTTP Header
- `-d`: 設定請求 Body (JSON 格式)
- `-k`: 忽略 SSL 憑證驗證 (開發環境用)
- `-i`: 顯示 Response Header
- `-s`: 靜默模式 (不顯示進度)

### API 參數
- `maxUsage`: Token 最大使用次數 (預設: 1)
- `expirationMinutes`: Token 過期時間，單位分鐘 (預設: 5)

---

## 📊 預期測試結果總覽

| 測試項目 | 預期 HTTP 狀態碼 | 說明 |
|---------|----------------|------|
| 缺少 Token | 401 Unauthorized | 基本防護 |
| 無效 Token | 401 Unauthorized | 防偽造 |
| 有效 Token (首次) | 200 OK | 正常流程 |
| Token 第二次使用 | 200 OK | 次數限制未達 |
| Token 第三次使用 | 401 Unauthorized | 超過使用次數 |
| 過期 Token | 401 Unauthorized | 時效控制 |
| 空白 Token | 401 Unauthorized | 邊界條件 |

---

## 🚀 快速執行

### Windows PowerShell (完整測試腳本)
建議另外建立 `curl-security-test.ps1` 或 `curl-security-test.sh` 腳本檔案，包含上述所有測試項目的自動化執行。

### Linux/macOS Bash
```bash
#!/bin/bash
# 請確保 API 服務已啟動在 https://localhost:7001
# 逐一執行上述 cURL 命令
```

---

## ⚠️ 注意事項

1. **執行前提**: 確保 WebAPI 服務已啟動 (`dotnet run` 於 Lab.CSRF2.WebAPI 目錄)
2. **SSL 憑證**: 開發環境使用 `-k` 參數略過憑證檢查
3. **Token 格式**: Token 為 GUID 格式，儲存於 Server 端 Memory Cache
4. **環境變數**: Linux/macOS 使用 `$TOKEN`，Windows PowerShell 使用 `$TOKEN` 或 `%TOKEN%` (cmd)
5. **換行符號**: Windows 的 `\r\n` 需使用 `tr -d '\r'` 清除

---

## 🎯 成功標準

所有 7 項測試項目的實際結果需符合預期結果：
- ✅ 4 項應拒絕存取 (HTTP 401)
- ✅ 2 項應允許存取 (HTTP 200)
- ✅ 1 項應拒絕存取 (HTTP 401 - 過期)

---

## 📝 後續建議

1. 建立自動化測試腳本 (PowerShell 或 Bash)
2. 整合至 CI/CD 流程
3. 加入效能測試 (大量並發請求)
4. 測試分散式環境下的 Token 同步問題
5. 加入 API Rate Limiting 測試
