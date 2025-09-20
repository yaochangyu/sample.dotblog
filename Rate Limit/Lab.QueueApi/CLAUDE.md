# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 專案概述

這是一個 ASP.NET Core 9 Web API 專案，實作了具有速率限制和佇列機制的排隊系統。專案使用滑動視窗演算法進行限流，超出限制的請求會自動進入佇列等待許可。**關鍵特色：調用端主動控制執行時機，背景服務只負責許可管理，不執行業務邏輯**。

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

### 業務流程

```mermaid
graph TD
    A[使用者請求 POST /api/commands] --> B[前置條件檢查]

    B --> C{檢查佇列是否已滿<br/>_commandQueue.IsQueueFull}
    C -->|佇列已滿| D[加入佇列並回傳 429<br/>Reason: QueueFull]

    C -->|佇列未滿| E{檢查限流<br/>!_rateLimiter.IsAllowed}
    E -->|限流失敗| F[加入佇列並回傳 429<br/>Reason: Rate Limit Exceeded]

    E -->|限流通過| G[記錄請求<br/>_rateLimiter.RecordRequest]
    G --> H[直接執行業務邏輯]
    H --> I[回傳 200 OK]

    D --> J[客戶端根據 RetryAfterSeconds 等待重試]
    F --> J
    J --> A
```

### 核心組件

1. **速率限制層 (Rate Limiting)**
   - `IRateLimiter` / `SlidingWindowRateLimiter`: 滑動視窗限流實作
   - 設定：每分鐘最多 2 個請求
   - 位置：`src/RateLimit/`

2. **佇列系統 (Queue System)**
   - `ICommandQueueProvider` / `ChannelCommandQueueProvider`: 請求佇列提供者
   - `ChannelCommandQueueService`: 背景服務**只負責許可管理**
   - 使用 .NET Channel 實作生產者-消費者模式
   - 位置：核心組件在 `src/RateLimit/`，背景服務在 `src/Services/`

3. **API 控制器**
   - `CommandController`: 主要 API 端點控制器
   - 路由：`/api/commands/*`, `/api/health`
   - 位置：`src/Commands/`

4. **清理和維護服務**
   - `ExpiredRequestCleanupService`: 過期請求清理服務
   - `CleanupRecord` / `CleanupSummaryResponse`: 清理記錄相關模型
   - 位置：`src/Services/` 和 `src/RateLimit/`

### 狀態管理

#### 狀態機圖

```mermaid
stateDiagram-v2
    [*] --> Queued : POST /api/commands<br/>(超過限流)

    Queued --> Ready : 背景服務<br/>許可管理
    Queued --> Failed : 系統錯誤<br/>或取消

    Ready --> Processing : 調用 /wait 端點<br/>開始執行
    Ready --> Failed : 執行失敗<br/>或超時

    Processing --> Finished : 執行完成<br/>移出佇列
    Processing --> Failed : 執行過程<br/>發生錯誤

    Failed --> [*] : 清理失敗記錄
    Finished --> [*] : 記錄到<br/>cleanup-summary

    note right of Queued
        在佇列中等待許可
        背景服務監控限流狀態
    end note

    note right of Ready
        已獲得執行許可
        等待調用端主動執行
        CanExecute = true
    end note

    note right of Processing
        正在執行業務邏輯
        由調用端控制執行
    end note

    note right of Finished
        執行完成並移出佇列
        記錄在 cleanup-summary
        可查詢歷史記錄
    end note

    note right of Failed
        執行失敗或被取消
        保留錯誤資訊
    end note
```

#### 狀態轉換說明

**主要路徑（成功流程）**：
`Queued` → `Ready` → `Processing` → `Finished`

**異常路徑**：
- `Queued` → `Failed`（系統錯誤）
- `Ready` → `Failed`（執行前失敗）
- `Processing` → `Failed`（執行中錯誤）

**關鍵控制點**：

1. **Queued → Ready**
   - 由背景服務 `ChannelCommandQueueService` 控制
   - 檢查限流條件，符合時自動轉換

2. **Ready → Processing**
   - 由調用端透過 `/wait` 端點主動觸發
   - 只有 `Ready` 狀態可執行

3. **Processing → Finished**
   - 業務邏輯執行完成後自動轉換
   - 請求從佇列移除，記錄到歷史

**搶票系統應用**：
- `Queued`: 排隊等待搶票許可
- `Ready`: 獲得搶票資格，可開始搶票
- `Processing`: 正在執行搶票動作
- `Finished`: 搶票完成（成功或失敗都算完成）

### 設計模式

- **依賴注入 (DI)**: 所有服務透過 ASP.NET Core DI 容器註冊
- **介面隔離**: 使用介面定義服務契約 (`IRateLimiter`, `ICommandQueueProvider`)
- **背景服務**: 使用 `IHostedService` 實作背景許可管理
- **Channel 模式**: 使用 .NET Channel 實作並發安全的佇列
- **調用端控制**: 使用者主動決定何時執行業務邏輯

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

### 主要端點

- `POST /api/commands` - 提交請求（支援前置條件檢查和排隊機制）
  - **前置條件檢查順序**：
    1. 檢查佇列是否已滿
    2. 檢查限流是否超限
  - **處理邏輯**：
    - 佇列已滿：加入佇列，回傳 429 (Reason: "QueueFull")
    - 限流失敗：加入佇列，回傳 429 (Reason: "Rate Limit Exceeded")
    - 都通過：直接執行業務邏輯，回傳 200 OK
  - **回傳資訊**：
    - RequestId：用於查詢佇列狀態
    - RetryAfterSeconds：建議重試時間
    - HTTP Headers：Retry-After, X-Queue-Position, X-Request-Id

- `GET /api/commands/{requestId}/status` - 查詢請求狀態
  - 回傳狀態：`Queued`, `Ready`, `Processing`, `Failed`, `Finished`
  - 包含 `CanExecute` 標示是否可執行

- `GET /api/commands/{requestId}/wait` - **執行業務邏輯**
  - 只有 `Ready` 狀態的請求可執行
  - 執行完成後狀態變為 `Finished` 並移出佇列
  - 非 `Ready` 狀態返回 429

### 輔助端點

- `GET /api/commands` - 列出所有佇列中的請求
- `GET /api/commands/cleanup-summary` - 查看所有完成的記錄
- `GET /api/health` - 系統健康檢查

## 專案結構

```
src/
├── Commands/           # API 控制器和請求/回應模型
│   ├── CommandController.cs       # 主要 API 端點控制器
│   ├── CreateCommandRequest.cs    # 建立命令請求模型
│   ├── GetCommandStatusResponse.cs # 命令狀態回應模型
│   └── QueuedCommandResponse.cs   # 佇列命令回應模型
├── RateLimit/          # 限流和佇列核心組件
│   ├── IRateLimiter.cs             # 限流器介面
│   ├── SlidingWindowRateLimiter.cs # 滑動視窗限流實作
│   ├── ICommandQueueProvider.cs    # 佇列提供者介面
│   ├── ChannelCommandQueueProvider.cs # Channel 佇列實作
│   ├── QueuedContext.cs            # 佇列上下文模型
│   ├── CleanupRecord.cs            # 清理記錄模型
│   └── CleanupSummaryResponse.cs   # 清理摘要回應模型
├── Services/           # 背景服務和清理服務
│   ├── ChannelCommandQueueService.cs    # 佇列背景處理服務
│   └── ExpiredRequestCleanupService.cs  # 過期請求清理服務
├── Properties/         # 專案配置檔案
│   └── launchSettings.json
├── appsettings.json            # 應用程式設定
├── appsettings.Development.json # 開發環境設定
├── Lab.QueueApi.csproj        # 專案檔案
├── Lab.QueueApi.http          # HTTP 測試檔案
└── Program.cs                 # 應用程式進入點和 DI 設定
```

## 開發注意事項

### 關鍵設計原則

1. **前置條件檢查機制**
   - 採用分階段檢查策略，優先檢查佇列容量
   - 任一檢查失敗都會將請求加入佇列並回傳 429
   - 提供明確的失敗原因和重試時機指引

2. **背景服務職責分離**
   - `ChannelCommandQueueService` **只負責許可管理**
   - 不執行業務邏輯，只將狀態從 `Queued` 改為 `Ready`
   - 調用端透過 `/wait` 端點主動執行業務邏輯

3. **狀態流轉控制**
   - 只有 `Ready` 狀態的請求可被執行
   - 執行後立即變為 `Finished` 並從佇列移除
   - 移除的請求記錄在 `cleanup-summary` 中

4. **限流和排隊策略**
   - 系統每分鐘處理 2 個請求（可調整）
   - 超出限制的請求自動進入佇列等待許可
   - 使用者可主動決定執行時機（調用 `/wait`）

### POST /api/commands 端點業務規則

1. **檢查順序**：
   ```
   步驟 1: 檢查佇列是否已滿 (_commandQueue.IsQueueFull())
   步驟 2: 檢查限流是否超限 (!_rateLimiter.IsAllowed())
   ```

2. **處理邏輯**：
   - 佇列已滿 → 加入佇列 → 回傳 429 (QueueFull)
   - 限流失敗 → 加入佇列 → 回傳 429 (Rate Limit Exceeded)
   - 都通過 → 記錄請求 → 直接執行 → 回傳 200

3. **429 回應格式**：
   ```json
   {
     "Message": "描述訊息",
     "RequestId": "唯一識別碼",
     "RetryAfterSeconds": 重試秒數,
     "QueueLength": 佇列長度,
     "MaxCapacity": 最大容量,
     "Reason": "失敗原因"
   }
   ```

4. **HTTP Headers**：
   - `Retry-After`: 建議重試時間（秒）
   - `X-Queue-Position`: 佇列位置
   - `X-Request-Id`: 請求識別碼

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
- 適當的 HTTP 狀態碼回應（200, 429, 404, 500）

## 測試策略

### 功能測試流程

使用提供的 `test_api.sh` 腳本進行完整功能測試：

1. **健康檢查端點測試**
   - 驗證系統基本運作

2. **前置條件檢查測試**
   - 佇列容量測試：當佇列滿時加入佇列並回傳 429 (QueueFull)
   - 限流機制測試：超出限制時加入佇列並回傳 429 (Rate Limit Exceeded)
   - 正常處理測試：通過檢查時直接執行並回傳 200

3. **狀態查詢測試**
   - 查詢佇列中請求的狀態變化：`Queued` → `Ready`

4. **主動執行測試**
   - 調用 `/wait` 端點執行業務邏輯
   - 驗證狀態：`Ready` → `Processing` → `Finished`
   - 確認請求從佇列中移除

5. **完成記錄驗證**
   - 透過 `/cleanup-summary` 查看完成的記錄

### 搶票系統測試場景

```bash
# 模擬前置條件檢查測試
curl -X POST /api/commands -d '{"data":"user1_ticket"}'  # 第1個請求：直接執行 (200)
curl -X POST /api/commands -d '{"data":"user2_ticket"}'  # 第2個請求：直接執行 (200)
curl -X POST /api/commands -d '{"data":"user3_ticket"}'  # 第3個請求：限流失敗，加入佇列 (429)

# 檢查佇列中的請求狀態
curl /api/commands/{requestId}/status  # 查看第3個請求狀態

# 等待背景服務處理或限流視窗重置
sleep 65  # 等待超過1分鐘

# 使用 wait 端點執行佇列中的請求
curl /api/commands/{requestId}/wait  # 執行第3個請求的業務邏輯
```

## 使用案例：搶票系統

### 完美適配場景

此系統專為搶票類應用設計，具備以下特色：

1. **公平排隊**：每分鐘只處理固定數量的請求
2. **用戶控制**：排隊用戶主動決定何時執行搶票
3. **狀態透明**：隨時查詢排隊狀態和位置
4. **歷史記錄**：完整的搶票記錄可查詢

### 實際應用流程

```
1. 演唱會開搶 → 大量用戶同時請求
2. 系統限流 → 前N個直接搶票成功，其餘收到 429 錯誤
3. 用戶重試 → 等待限流視窗重置後重新發送請求
4. 佇列保護 → 當系統負載過重時，佇列滿則拒絕請求
5. 成功處理 → 通過檢查的請求直接執行搶票邏輯
```

### 技術優勢

- **避免系統崩潰**：限流機制保護後端服務
- **用戶體驗佳**：排隊狀態透明，主動控制執行
- **可擴展性強**：分離架構易於水平擴展
- **數據完整性**：完整的操作日誌和歷史記錄

## 部署相關

- 支援多種部署方式：開發環境、Docker、Linux 服務、雲端部署
- 詳細部署指南請參考 `DEPLOYMENT.md`
- 生產環境需要關閉 Swagger UI 並設定適當的日誌等級

---

## 重要提醒

**此系統的核心設計理念**：
- 背景服務只負責許可管理，不執行業務邏輯
- 調用端透過 `/wait` 端點主動控制執行時機
- 適合需要用戶主動控制的場景（如搶票、預約等）