# WebAPI 防濫用機制實作計畫

## 實作步驟

### 階段一：專案建立與基礎設定

- [ ] **建立 dotnet solution**
  - 目的：建立方案架構，統一管理專案
  - 必要性：符合 .NET 專案最佳實務，方便專案管理
  - 命令：`dotnet new sln -n Lab.CSRF2`

- [ ] **建立 ASP.NET Core Web API .NET 10 專案**
  - 目的：建立基礎 API 專案架構
  - 必要性：作為後續功能開發的基礎
  - 命令：`dotnet new webapi -n Lab.CSRF2.WebAPI -f net10.0`

- [ ] **建立 .gitignore 檔案**
  - 目的：排除不需版控的檔案（bin、obj、暫存檔等）
  - 必要性：保持版本庫乾淨，避免提交編譯產物
  - 檔案：.gitignore（使用 ASP.NET Core 範本）

- [ ] **將專案加入方案**
  - 目的：將 Web API 專案加入方案管理
  - 必要性：方案化管理專案
  - 命令：`dotnet sln add Lab.CSRF2.WebAPI\Lab.CSRF2.WebAPI.csproj`

- [ ] **設定 CORS 政策**
  - 目的：允許瀏覽器跨域存取 API
  - 必要性：瀏覽器場景必須支援 CORS
  - 修改：Program.cs 加入 CORS 設定

- [ ] **加入 Memory Cache 套件**
  - 目的：儲存 Token 資料
  - 必要性：Token 驗證需要 Server 端儲存機制
  - 套件：Microsoft.Extensions.Caching.Memory (通常已內建)

### 階段二：Token 管理機制

- [ ] **建立 Token 產生服務 (ITokenService)**
  - 目的：封裝 Token 產生邏輯
  - 必要性：遵循單一職責原則，方便測試與維護
  - 檔案：Services/ITokenService.cs, Services/TokenService.cs

- [ ] **實作 Token 儲存機制**
  - 目的：將 Token 存入 Memory Cache，包含過期時間與使用次數
  - 必要性：驗證時需要比對 Server 端的 Token
  - 實作：TokenService 中使用 IMemoryCache

- [ ] **實作 Token 驗證邏輯**
  - 目的：驗證 Token 有效性、過期時間、使用次數
  - 必要性：核心安全防護機制
  - 實作：TokenService.ValidateToken 方法

### 階段三：API 端點實作

- [ ] **實作 GET /api/token 端點**
  - 目的：產生並回傳 Token 至 Response Header
  - 必要性：客戶端取得 Token 的入口
  - 檔案：Controllers/TokenController.cs

- [ ] **建立 Token 驗證 ActionFilter**
  - 目的：統一處理 Token 驗證邏輯
  - 必要性：避免每個端點重複驗證程式碼
  - 檔案：Filters/ValidateTokenAttribute.cs

- [ ] **實作 POST /api/protected 端點**
  - 目的：受保護的 API，需驗證 Token
  - 必要性：示範 Token 驗證機制的實際應用
  - 檔案：Controllers/ProtectedController.cs

### 階段四：客戶端範例

- [ ] **建立 cURL 呼叫範例腳本**
  - 目的：提供命令列測試方式
  - 必要性：方便開發與測試驗證
  - 檔案：test-api.sh (bash) 或 test-api.ps1 (PowerShell)

- [ ] **建立 HTML + JavaScript 範例頁面**
  - 目的：示範瀏覽器如何呼叫 API
  - 必要性：瀏覽器場景的實作參考
  - 檔案：wwwroot/test.html

### 階段五：安全性測試

- [ ] **測試 Token 過期情境**
  - 目的：驗證過期 Token 無法通過驗證
  - 必要性：確保時效性限制有效
  - 方式：設定短過期時間，等待後測試

- [ ] **測試使用次數限制**
  - 目的：驗證 Token 使用次數超過限制後失效
  - 必要性：確保使用次數限制有效
  - 方式：連續呼叫 API 驗證次數限制

- [ ] **測試無效 Token**
  - 目的：驗證錯誤或偽造的 Token 被拒絕
  - 必要性：確保基本安全防護
  - 方式：使用隨機 GUID 測試

- [ ] **測試未帶 Token 的請求**
  - 目的：驗證缺少 Token 的請求被拒絕
  - 必要性：確保強制驗證機制有效
  - 方式：不帶 Header 呼叫 API

### 階段六：文件與清理

- [ ] **建立 README.md 說明文件**
  - 目的：提供使用說明與架構說明
  - 必要性：方便他人理解與使用
  - 檔案：README.md

- [ ] **更新 @tree.md 專案結構**
  - 目的：記錄專案檔案結構
  - 必要性：符合技術規範要求
  - 檔案：@tree.md

---

## 技術選型

- **框架**: ASP.NET Core Web API (.NET 10)
- **方案管理**: .NET Solution (.sln)
- **Token 儲存**: IMemoryCache
- **Token 格式**: GUID
- **驗證方式**: Custom ActionFilter
- **CORS**: 開發環境允許所有來源

## 預期產出

1. ✅ .NET Solution 方案檔
2. ✅ 完整可執行的 Web API 專案 (.NET 10)
3. ✅ Token 產生與驗證機制
4. ✅ cURL/PowerShell 測試腳本
5. ✅ HTML/JS 測試頁面
6. ✅ 安全性測試案例
7. ✅ 使用說明文件
8. ✅ @tree.md 專案結構記錄

---

## 執行模式說明

- **預設模式（逐步確認）**：每完成一步驟後等待您的確認再繼續
- **自動模式（連續執行）**：完成所有步驟，不中斷等待確認

---

**請確認此計畫後，我將開始實作。**  
**執行模式：預設為逐步確認，如需自動執行請告知。**
