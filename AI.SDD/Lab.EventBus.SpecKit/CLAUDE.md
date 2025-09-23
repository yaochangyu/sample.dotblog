# Lab.EventBus.SpecKit Development Context for Claude Code

**Last updated**: 2025-09-23

## Current Feature Development

**Feature**: Task Management Platform 集中管理平台
**Branch**: 001-task-management-platform
**Status**: Phase 1 - Design & Contracts Complete

## Project Overview

這是一個集中化任務管理平台的實作專案，提供 RESTful API 來建立、處理和追蹤任務執行狀態。專案採用 Clean Architecture 原則，參考 https://github.com/yaochangyu/api.template 的編碼規範。

## Active Technologies

- C# 11, .NET 8 (001-task-management-platform)
- ASP.NET Core 8.0, Entity Framework Core, MediatR (001-task-management-platform)
- SQL Server 或 PostgreSQL (001-task-management-platform)

## Architecture & Design Principles

### Clean Architecture 四層結構
```
Lab.TaskManagement.Domain/          # 領域層 - 核心業務邏輯
Lab.TaskManagement.Application/     # 應用層 - 用例實作
Lab.TaskManagement.Infrastructure/  # 基礎設施層 - 外部整合
Lab.TaskManagement.WebApi/         # 展示層 - API 控制器
```

### 核心設計原則
- 依賴反轉：高層模組不依賴低層模組
- 關注點分離：每層專注於特定職責
- 測試驅動開發：紅綠重構循環
- 領域驅動設計：以業務邏輯為中心
- CQRS 模式：命令查詢職責分離

### 實作慣例
1. **實體設計**: 使用 Factory Method 建立實體，封裝業務規則
2. **狀態管理**: 使用強型別 Enum 和狀態轉換方法
3. **錯誤處理**: 使用 Result Pattern，避免異常作為控制流程
4. **並行控制**: 樂觀並行控制結合資料庫鎖定
5. **事件處理**: 領域事件實現鬆耦合通訊

## Current Project Structure

```
specs/001-task-management-platform/
├── spec.md              # 功能規格文件
├── plan.md              # 實作計畫
├── research.md          # 技術研究報告
├── data-model.md        # 資料模型設計
├── quickstart.md        # 快速開始指南
├── contracts/           # API 契約
│   └── openapi.yaml     # OpenAPI 規格
└── tasks.md             # 實作任務清單 (待生成)

src/ (規劃中)
├── Lab.TaskManagement.Domain/
├── Lab.TaskManagement.Application/
├── Lab.TaskManagement.Infrastructure/
└── Lab.TaskManagement.WebApi/

tests/ (規劃中)
├── Lab.TaskManagement.Domain.Tests/
├── Lab.TaskManagement.Application.Tests/
├── Lab.TaskManagement.Infrastructure.Tests/
├── Lab.TaskManagement.WebApi.Tests/
└── Lab.TaskManagement.Integration.Tests/
```

## Build & Test Commands

```bash
# 建置專案
dotnet build src/Lab.TaskManagement.sln

# 執行測試
dotnet test tests/ --configuration Release

# 程式碼品質檢查
dotnet format --verify-no-changes
dotnet run --project tools/CodeAnalysis
```

## API 端點概覽

### 核心 API
- `POST /api/v1/tasks` - 建立任務
- `GET /api/v1/tasks/dequeue` - 取出任務
- `POST /api/v1/tasks/{id}/execute` - 執行任務
- `GET /api/v1/tasks/{id}` - 查詢任務狀態

### 狀態流程
```
Queued → Dequeued → Processing → Completed/Failed
                     ↓
                   Retry → Processing (if < maxRetries)
```

## Key Development Guidelines

### 編碼標準
1. **命名規範**: 使用 PascalCase (類別、方法)、camelCase (參數、區域變數)
2. **註解規範**: 使用 XML 文檔註解，說明業務意圖而非實作細節
3. **測試命名**: Given_When_Then 模式或 Should_ExpectedBehavior_When_StateUnderTest
4. **檔案組織**: 一個檔案一個公開類別，內部類別可例外

### 測試策略
1. **單元測試**: 每個業務方法都有對應測試
2. **整合測試**: API 端點和資料庫互動
3. **契約測試**: API 請求/回應格式驗證
4. **效能測試**: 關鍵路徑的負載測試

### 錯誤處理慣例
```csharp
// 使用 Result Pattern
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
}

// 業務邏輯錯誤
public static Result<TaskEntity> CreateTask(string taskData, string callbackUrl)
{
    if (string.IsNullOrWhiteSpace(taskData))
        return Result<TaskEntity>.Failure("TaskData cannot be empty");

    var task = TaskEntity.Create(taskData, callbackUrl);
    return Result<TaskEntity>.Success(task);
}
```

## Recent Changes

- 001-task-management-platform: Added C# 11, .NET 8 + ASP.NET Core 8.0, Entity Framework Core, MediatR
- 完成系統流程圖、狀態機和循序圖設計 (mermaid 格式)
- 完成 Phase 0 技術研究和 Phase 1 設計階段
- 建立完整的 OpenAPI 契約和資料模型設計

## Important Notes

### 特殊要求
- 編碼原則參考 https://github.com/yaochangyu/api.template CLAUDE.md
- 實作時要從 api.template 複製並修改命名空間為 `Lab.TaskManagement.*`
- 所有圖表使用 mermaid 格式編寫
- 遵循 Clean Architecture 原則和 TDD 開發方法

### 效能目標
- 支援 1000+ req/s 的任務處理能力
- 任務執行回應時間 <5s
- 記憶體使用 <500MB per instance
- 支援 10k+ 併發任務處理

### 安全考量
- HTTPS 強制要求
- Input validation 和 sanitization
- 結構化日誌但避免敏感資料洩露
- 回調 URL 安全性驗證

## Phase Status
- [x] Phase 0: Research complete
- [x] Phase 1: Design complete
- [ ] Phase 2: Task planning (待 /tasks 命令)
- [ ] Phase 3: Implementation
- [ ] Phase 4: Testing & Validation

使用 `/plan` 和 `/tasks` 命令繼續開發流程。