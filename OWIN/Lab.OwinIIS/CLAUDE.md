# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 語言規範
請使用**台灣用語的繁體中文**回覆所有問題和互動。

## 開發環境
- **作業系統**：Windows Pro
- **指令碼環境**：PowerShell (跨平台版本)
- **建置工具**：MSBuild
- **測試工具**：VSTest Console

## 建置和執行指令

### 建置方案
```bash
# 建置整個方案
msbuild Lab.OwinIIS.sln

# 或建置特定專案
msbuild AspNetFx.WebApi\AspNetFx.WebApi.csproj
msbuild AspNetFx.WebApi.Test\AspNetFx.WebApi.Test.csproj
```

#### msbuild 的位置
- C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe
- C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe
- C:\Users\yao\scoop\apps\rider\2025.2-252.23892.524\IDE\tools\MSBuild\Current\Bin\MSBuild.exe
- C:\Users\yao\scoop\apps\rider\2025.2-252.23892.524\IDE\tools\MSBuild\Current\Bin\amd64\MSBuild.exe

### 執行測試
```bash
# 使用 MSTest 執行所有測試
vstest.console.exe AspNetFx.WebApi.Test\bin\Debug\AspNetFx.WebApi.Test.dll

# 或使用 Visual Studio Test Explorer
```
#### vstest.console.exe 的位置
- C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe
- C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe

### 執行 Web API
專案可以兩種模式執行：
- **IIS Express**：設定在 `http://localhost:51438/` 執行（參考 AspNetFx.WebApi.csproj）
- **自主裝載**：測試使用 OWIN 自主裝載在 `http://localhost:8001`（參考 UnitTest1.cs）

## 專案架構

這是一個雙架構的 ASP.NET Web API 專案，展示傳統 ASP.NET 和 OWIN 裝載兩種方式：

### 核心專案
- **AspNetFx.WebApi**：主要 Web API 專案，支援 ASP.NET 和 OWIN 兩種啟動設定
- **AspNetFx.WebApi.Test**：MSTest 單元測試專案，使用 OWIN 自主裝載進行整合測試

### 關鍵架構元件

#### HTTP 內容抽象層
專案實作了 HTTP 內容存取的抽象層：
- `IHttpContextProvider`：定義 HTTP 內容操作的介面
- `HttpContextProvider`：使用 `HttpContext.Current` 的 ASP.NET 實作
- `OWinHttpContextProvider`：OWIN 實作（目前為未完成的存根，拋出 NotImplementedException）

#### OWIN 設定
- `Startup.cs`：OWIN 啟動類別，設定 Web API 路由
- 使用 `Microsoft.Owin` 及相關套件提供自主裝載功能
- Web.config 包含 `owin:appStartup` 設定，指向 Startup 類別

#### 框架版本
- **目標框架**：.NET Framework 4.7.2
- **ASP.NET Web API**：5.3.0
- **OWIN**：4.2.x 系列
- **MSTest**：1.4.0 用於測試

### 測試策略
- 整合測試使用 OWIN 自主裝載（`WebApp.Start<Startup>`）
- 測試驗證 API 端點回傳預期的 HTTP 狀態碼和內容
- 組件層級的設定/清理管理測試網頁伺服器的生命週期

### 開發注意事項
- 專案展示支援 IIS 裝載和自主裝載 Web API 情境的常見模式
- HTTP 內容提供者允許在 ASP.NET 和 OWIN 內容之間切換
- 目前的 OWIN HTTP 內容提供者尚未完成，需要實作
- 程式寫完後，一定要執行測試，確保所有功能正常運作