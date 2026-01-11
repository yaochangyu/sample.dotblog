# 前端頁面 CSRF 防護測試計畫

## 測試目標
驗證 `/api/protected` 端點是否具備完整的 CSRF 防護能力，使用前端頁面進行模擬攻擊測試。

## 測試項目

### ✅ 1. 正常流程測試
**目的**: 驗證合法請求能正常通過
- [ ] 取得有效 Token (GET /api/token)
- [ ] 使用正確 Token 呼叫受保護 API (POST /api/protected)
- [ ] 預期結果: HTTP 200 OK

### ✅ 2. 缺少 Token 測試
**目的**: 驗證未攜帶 Token 的請求會被拒絕
- [ ] 不帶 X-CSRF-Token Header 直接呼叫 /api/protected
- [ ] 預期結果: HTTP 401 Unauthorized 或 403 Forbidden

### ✅ 3. 無效 Token 測試
**目的**: 驗證偽造或無效的 Token 會被拒絕
- [ ] 使用偽造的 Token (例如: fake-token-12345) 呼叫 /api/protected
- [ ] 預期結果: HTTP 401 Unauthorized

### ✅ 4. Token 重複使用測試
**目的**: 驗證 Token 使用次數限制
- [ ] 取得限制使用 1 次的 Token
- [ ] 第一次呼叫成功 (HTTP 200)
- [ ] 第二次呼叫失敗 (HTTP 401)
- [ ] 驗證 Server 端正確追蹤使用次數

### ✅ 5. Token 過期測試
**目的**: 驗證過期 Token 會被拒絕
- [ ] 取得極短過期時間的 Token (例如: 0.1 分鐘)
- [ ] 等待 Token 過期
- [ ] 使用過期 Token 呼叫 API
- [ ] 預期結果: HTTP 401 Unauthorized

### ✅ 6. 跨站請求偽造模擬測試
**目的**: 模擬真實 CSRF 攻擊場景
- [ ] 建立惡意 HTML 頁面
- [ ] 模擬在其他網站嵌入表單或 JavaScript 攻擊
- [ ] 嘗試無 Token 情況下呼叫受保護 API
- [ ] 預期結果: 請求被拒絕

### ✅ 7. 不同來源 Token 測試
**目的**: 驗證 Token 與請求的關聯性
- [ ] 使用者 A 取得 Token
- [ ] 模擬使用者 B 使用該 Token 呼叫 API
- [ ] 驗證是否能正確阻擋 (若有實作 Session 綁定)

### ✅ 8. 並發請求測試
**目的**: 驗證同時多個請求的處理
- [ ] 取得 maxUsage=3 的 Token
- [ ] 同時發送 5 個請求
- [ ] 驗證只有 3 個成功,其餘失敗

## 測試環境需求
- ✅ WebAPI 服務已啟動 (https://localhost:7001)
- ✅ 瀏覽器支援 Fetch API
- ✅ 測試頁面路徑: https://localhost:7001/test.html

## 執行方式
1. 啟動 WebAPI 服務
2. 開啟測試頁面 (https://localhost:7001/csrf-test.html)
3. 依序執行各測試項目
4. 記錄測試結果

## 成功標準
- ✅ 所有惡意請求 (缺少/無效/過期 Token) 均被拒絕
- ✅ 合法請求正常通過
- ✅ Token 使用次數限制正確執行
- ✅ 無法透過模擬攻擊繞過防護

## 風險評估
- **高風險**: 若任何測試項目失敗,表示存在 CSRF 漏洞
- **中風險**: Token 儲存機制不當可能導致資訊洩漏
- **低風險**: CORS 設定過於寬鬆 (已知開發環境限制)
