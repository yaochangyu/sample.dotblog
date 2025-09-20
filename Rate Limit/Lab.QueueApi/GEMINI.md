# GEMINI.md

## 專案概觀

本專案是一個 .NET 9 Web API，用於展示速率限制和佇列機制。它的設計目標是每分鐘最多處理 2 個請求，任何超出的請求都會被放入佇列中以便稍後處理。此 API 會對排入佇列的請求回傳 429 (Too Many Requests) 狀態碼，並提供一個機制讓客戶端查詢其請求的狀態。

專案的核心組件包括：

*   **`SlidingWindowRateLimiter`**：實作滑動視窗速率限制演算法，以控制在特定時間範圍內處理的請求數量。
*   **`ChannelRequestQueueProvider` 和 `ChannelRequestQueueService`**：一個使用 .NET Channels 建構的佇列系統，用於管理超出速率限制的請求。一個背景服務會處理這些排入佇列的請求。
*   **`QueueController`**：主要的 API 控制器，提供用於處理請求、查詢請求狀態以及監控系統健康狀況的端點。

## 建置與執行

### 先決條件

*   .NET 9.0 SDK
*   ASP.NET Core 9.0

### 建置專案

```bash
dotnet build
```

### 執行專案

```bash
dotnet run
```

API 將在 `http://localhost:5001` 上提供服務。在開發環境中，Swagger UI 將在根 URL 上提供，用於 API 文件和測試。

### 測試

專案包含一個測試腳本 `test_api.sh`，可用於測試 API 的所有功能。

```bash
chmod +x test_api.sh
./test_api.sh
```

## 開發慣例

*   **程式碼風格**：專案遵循標準的 C# 和 ASP.NET Core 慣例。
*   **依賴注入**：服務是使用 .NET 的內建依賴注入框架進行註冊和取用。
*   **組態設定**：速率限制和佇列容量可以在 `Program.cs` 中進行設定。
*   **API 文件**：XML 註解用於透過 Swagger 產生詳細的 API 文件。

# 注意事項
*   使用台灣用語的繁體中文，寫註解、文件、回答問題