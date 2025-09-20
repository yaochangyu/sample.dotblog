# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

這是一個 ASP.NET Core 9 Web API 專案，實作了具有速率限制和佇列機制的排隊系統。專案使用滑動視窗演算法進行限流，超出限制的請求會自動進入佇列等待背景處理。

## 建置和執行命令

### 建置專案
```bash
dotnet build
```

### 執行專案（開發模式）
```bash
dotnet run
```
API 將在 `http://localhost:5001` 啟動，Swagger UI 可在根路徑存取。

### 執行測試
```bash
# 執行自動化測試腳本
chmod +x test_api.sh
./test_api.sh
```

### 發布建置
```bash
# 建置發布版本
dotnet publish -c Release -o ./publish

# 建置自包含版本
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
```

## 架構設計

### 核心組件

1. **速率限制層 (Rate Limiting)**
   - `IRateLimiter` / `SlidingWindowRateLimiter`: 滑動視窗限流實作
   - 設定：每分鐘最多 2 個請求
   - 位置：`src/Services/`

2. **佇列系統 (Queue System)**
   - `ICommandQueueProvider` / `ChannelCommandQueueProvider`: 請求佇列提供者
   - `ChannelCommandQueueService`: 背景服務處理排隊請求
   - 使用 .NET Channel 實作生產者-消費者模式
   - 位置：`src/Services/`

3. **API 控制器**
   - `CommandController`: 主要 API 端點控制器
   - 路由：`/api/commands`, `/api/health`
   - 位置：`src/Commands/`

### 設計模式

- **依賴注入 (DI)**: 所有服務透過 ASP.NET Core DI 容器註冊
- **介面隔離**: 使用介面定義服務契約 (`IRateLimiter`, `ICommandQueueProvider`)
- **背景服務**: 使用 `IHostedService` 實作背景請求處理
- **Channel 模式**: 使用 .NET Channel 實作並發安全的佇列

### 重要配置

在 `Program.cs` 中的關鍵服務註冊：
```csharp
// 限流器設定（每分鐘 2 個請求）
builder.Services.AddSingleton<IRateLimiter>(provider =>
    new SlidingWindowRateLimiter(maxRequests: 2, timeWindow: TimeSpan.FromMinutes(1)));

// 佇列提供者（容量 100）
builder.Services.AddSingleton<ICommandQueueProvider>(provider =>
    new ChannelCommandQueueProvider(capacity: 100));

// 背景服務
builder.Services.AddHostedService<ChannelCommandQueueService>();
```

## API 端點

- `POST /api/commands` - 提交請求（支援限流和排隊）
- `GET /api/commands/{requestId}/status` - 查詢請求狀態
- `GET /api/commands/{requestId}/wait` - 等待請求完成
- `GET /api/commands` - 列出所有佇列中的請求
- `GET /api/health` - 系統健康檢查

## 專案結構

```
src/
├── Commands/           # API 控制器和請求/回應模型
│   ├── CommandController.cs
│   ├── CreateCommandRequest.cs
│   ├── QueuedCommandResponse.cs
│   └── QueuedContext.cs
├── Services/           # 核心業務邏輯服務
│   ├── IRateLimiter.cs
│   ├── SlidingWindowRateLimiter.cs
│   ├── ICommandQueueProvider.cs
│   ├── ChannelCommandQueueProvider.cs
│   └── ChannelCommandQueueService.cs
└── Program.cs          # 應用程式進入點和 DI 設定
```

## 開發注意事項

### 修改限流設定
若需調整限流參數，修改 `Program.cs` 中的 `SlidingWindowRateLimiter` 設定：
```csharp
new SlidingWindowRateLimiter(maxRequests: 5, timeWindow: TimeSpan.FromMinutes(1))
```

### 修改佇列容量
調整 `ChannelCommandQueueProvider` 的容量參數：
```csharp
new ChannelCommandQueueProvider(capacity: 200)
```

### 並發安全
- 所有服務都設計為執行緒安全
- 使用 `ConcurrentQueue` 和 `ConcurrentDictionary` 確保並發操作安全
- Channel 提供了內建的並發安全保證

### 錯誤處理
- 所有控制器方法都包含完整的 try-catch 錯誤處理
- 使用結構化日誌記錄關鍵操作和錯誤
- 適當的 HTTP 狀態碼回應（200, 202, 429, 404, 408, 500）

## 測試策略

使用提供的 `test_api.sh` 腳本進行功能測試：
1. 健康檢查端點測試
2. 前兩個請求的直接處理驗證
3. 第三個請求的排隊機制驗證（429 狀態碼）
4. 請求狀態查詢功能測試
5. 背景處理完成驗證

## 部署相關

- 支援多種部署方式：開發環境、Docker、Linux 服務、雲端部署
- 詳細部署指南請參考 `DEPLOYMENT.md`
- 生產環境需要關閉 Swagger UI 並設定適當的日誌等級