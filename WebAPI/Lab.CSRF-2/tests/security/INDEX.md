# 🔒 CSRF 防護 API 安全性測試完整套件

## 📚 文件導覽

### 快速開始
- **[QUICK-REFERENCE.md](QUICK-REFERENCE.md)** - 快速參考指南（最快上手）
- **[README.md](README.md)** - 詳細執行指南

### 測試計畫
- **[security-test-plan.md](security-test-plan.md)** - 完整測試計畫（12 個測試案例）

### 技術說明
- **[SECURITY-MECHANISMS.md](SECURITY-MECHANISMS.md)** - 安全機制完整說明
- **[SUMMARY.md](SUMMARY.md)** - 專案完成摘要

## 🎯 核心功能

### API 端點保護
- ✅ **只能被當前頁面使用** - 透過 User-Agent + Referer/Origin 驗證
- ✅ **防止爬蟲濫用** - User-Agent 黑名單 + 速率限制
- ✅ **curl 無法直接存取** - 多重驗證機制

### Token 安全機制
- ✅ **有效期限** - 預設 5 分鐘，可配置
- ✅ **使用次數限制** - 預設 1 次，可配置
- ✅ **客戶端綁定** - User-Agent 一致性驗證

### 多層防護
1. 速率限制 (Rate Limiting)
2. User-Agent 黑名單
3. Referer/Origin 白名單
4. Token 存在性驗證
5. Token 有效性驗證
6. Token 過期驗證
7. Token 使用次數驗證
8. User-Agent 一致性驗證

## 📊 測試統計

- **測試案例**: 12 個
- **測試腳本**: 27 個
  - Bash 腳本: 10 個
  - PowerShell 腳本: 10 個
  - Playwright 測試: 3 個
- **說明文件**: 5 個

## 🚀 3 分鐘快速開始

### 步驟 1: 啟動 API 伺服器
```bash
cd ../../Lab.CSRF2.WebAPI
dotnet run
```

### 步驟 2: 執行測試

**選項 A: curl 測試（推薦從這裡開始）**
```bash
# Linux/macOS
chmod +x scripts/*.sh
./scripts/run-all-tests.sh

# Windows
.\scripts\run-all-tests.ps1
```

**選項 B: 瀏覽器測試**
```bash
npm install
npx playwright install
npm test
```

## 📁 專案結構

```
tests/security/
├── 📖 INDEX.md (本檔案)
├── 📄 QUICK-REFERENCE.md       # 快速參考
├── 📄 README.md                # 詳細指南
├── 📄 security-test-plan.md    # 測試計畫
├── 📄 SECURITY-MECHANISMS.md   # 安全機制說明
├── 📄 SUMMARY.md               # 完成摘要
├── 📦 package.json
├── ⚙️  playwright.config.js
└── 📂 scripts/
    ├── 🔧 run-all-tests.sh/ps1        # 主執行腳本
    ├── ✅ test-01-normal-flow         # 正常流程測試
    ├── ⏱️  test-02-token-expiration   # Token 過期測試
    ├── 🔢 test-03-usage-limit         # 使用次數限制測試
    ├── ❌ test-04-missing-token       # 無 Token 測試
    ├── 🚫 test-05-invalid-token       # 無效 Token 測試
    ├── 👤 test-06-ua-mismatch         # User-Agent 不一致測試
    ├── ⚡ test-07-rate-limiting       # 速率限制測試
    ├── 🌐 test-08-browser-normal      # 瀏覽器正常流程
    ├── 🌐 test-09-browser-usage-limit # 瀏覽器使用限制
    ├── 🌐 test-10-cross-page          # 跨頁面測試
    ├── 💥 test-11-direct-attack       # 直接攻擊測試
    └── 🔁 test-12-replay-attack       # 重放攻擊測試
```

## 🧪 測試類型

### 基本功能測試 (test-01 ~ test-03)
驗證 Token 的基本運作機制

### 安全防護測試 (test-04 ~ test-07)
驗證各種安全防護機制

### 瀏覽器整合測試 (test-08 ~ test-10)
使用 Playwright 驗證瀏覽器環境

### 攻擊防護測試 (test-11 ~ test-12)
模擬真實攻擊場景

## 📖 建議閱讀順序

### 新手入門
1. **[QUICK-REFERENCE.md](QUICK-REFERENCE.md)** - 快速了解如何執行測試
2. **執行測試** - 實際跑一次看看結果
3. **[security-test-plan.md](security-test-plan.md)** - 了解每個測試在測什麼

### 深入理解
4. **[SECURITY-MECHANISMS.md](SECURITY-MECHANISMS.md)** - 了解背後的安全機制
5. **查看程式碼** - 研究 API 實作細節
6. **[README.md](README.md)** - 學習進階測試技巧

### 專案管理
7. **[SUMMARY.md](SUMMARY.md)** - 了解專案完整內容
8. **自訂測試** - 根據需求調整測試案例

## 💡 使用建議

### 開發階段
- 每次修改 API 後執行 `run-all-tests.sh`
- 關注失敗的測試案例
- 使用 Playwright UI 模式除錯瀏覽器測試

### 測試階段
- 執行完整測試套件
- 檢視測試報告
- 驗證所有安全機制

### 生產前
- 確認所有測試通過
- 檢查 [SECURITY-MECHANISMS.md](SECURITY-MECHANISMS.md) 中的生產環境建議
- 啟用額外的安全設定（IP 綁定、強制 HTTPS 等）

## 🔗 相關資源

### API 端點
- Token 生成: `GET /api/token`
- 受保護 API: `POST /api/protected`
- 測試頁面: `http://localhost:5073/test.html`

### 設定檔
- API 專案: `../../Lab.CSRF2.WebAPI/Program.cs`
- Token 服務: `../../Lab.CSRF2.WebAPI/Services/TokenService.cs`
- 驗證過濾器: `../../Lab.CSRF2.WebAPI/Filters/ValidateTokenAttribute.cs`

## 🎓 學習路徑

```
1. 快速開始（5 分鐘）
   └─> 執行 run-all-tests.sh
   
2. 理解測試（15 分鐘）
   └─> 閱讀 security-test-plan.md
   
3. 深入機制（30 分鐘）
   └─> 閱讀 SECURITY-MECHANISMS.md
   
4. 實作研究（1 小時）
   └─> 研究 API 原始碼
   
5. 自訂擴展（依需求）
   └─> 調整測試案例或安全機制
```

## ✅ 完成檢查清單

- [ ] 閱讀快速參考指南
- [ ] 啟動 API 伺服器
- [ ] 執行 curl 測試
- [ ] 執行 Playwright 測試
- [ ] 理解每個測試案例
- [ ] 了解安全機制運作
- [ ] 查看 API 實作細節
- [ ] 思考如何應用到專案

## 🆘 需要協助？

1. **測試執行問題** → 查看 [README.md](README.md) 的疑難排解章節
2. **不理解測試目的** → 查看 [security-test-plan.md](security-test-plan.md)
3. **想了解實作細節** → 查看 [SECURITY-MECHANISMS.md](SECURITY-MECHANISMS.md)
4. **快速查詢** → 使用 [QUICK-REFERENCE.md](QUICK-REFERENCE.md)

---

**🎉 歡迎使用 CSRF 防護 API 安全性測試套件！**

從 [QUICK-REFERENCE.md](QUICK-REFERENCE.md) 開始，3 分鐘內即可完成第一次測試。
