# Idempotency Key — 分散式冪等保護範例

在多 Pod / Container 部署環境下，透過 **Redis** 或 **PostgreSQL** 實現分散式冪等保護，防止網路重試造成重複寫入。

搭配文章：[升級版 Idempotency Key：用 Redis 實現分散式冪等保護](blog-claude.md)

---

## 技術棧

- **.NET 10** / ASP.NET Core Web API
- **Redis 7**（主要儲存層）
- **PostgreSQL 17** + EF Core（替代儲存層）
- **Docker Compose**（本機開發環境）
- **[Task](https://taskfile.dev/)**（自動化測試腳本）

---

## 快速啟動

### 前置需求

- Docker
- .NET 10 SDK
- [Task](https://taskfile.dev/#/installation)（`brew install go-task` / `winget install Task.Task`）

### 一鍵執行完整測試

```bash
task test:all
```

這個指令會依序：
1. 啟動 PostgreSQL + Redis 容器並等待 Healthcheck
2. 還原 NuGet 套件 + 執行 EF Core Migration
3. 建置專案
4. 啟動 Pod1（port 5048）+ Pod2（port 5049）
5. 執行功能測試（httpyac .http）
6. 執行分散式鎖測試（兩個 Pod 並發）
7. 清除容器

### 其他常用指令

```bash
task docker:up          # 啟動容器
task data:init          # 還原套件 + EF Migration
task api:build          # 建置專案
task api:start          # 啟動 Pod1（port 5048）
task api:start-pod2     # 啟動 Pod2（port 5049，模擬多副本）
task test               # 執行 .http 功能測試
task test:distributed   # 執行分散式鎖整合測試
task test:stress:all    # 50 RPS 壓力測試（驗證高並發下不重複寫入）
task docker:down        # 停止並清除容器
```

---

## 連線設定

`src/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=member;Username=postgres;Password=postgres",
    "Redis": "localhost:6380"
  }
}
```

Docker Compose 預設將 Redis 對外 port 設為 `6380`（避免與本機衝突），PostgreSQL 為 `5432`。

---

## 架構說明

### IIdempotencyKeyStore 介面

兩種可互換的儲存層實作，透過 DI 注入，`Program.cs` 預設使用 Redis：

| 實作 | 原子鎖機制 | 備註 |
|------|-----------|------|
| `RedisIdempotencyKeyStore` | `SET NX EX` | 主要實作；支援 TTL 自動過期 |
| `EfIdempotencyKeyStore` | PostgreSQL Unique Constraint（`23505`）| 不想維運 Redis 時的替代方案 |

### 處理狀態

```
不存在 → InProgress（短 TTL 鎖）→ Completed / Failed（長 TTL 快取）
                ↑
          5xx / 未處理例外 / IsRetryable → 刪除 Key（讓客戶端重試）
```

### [IdempotencyKey] Action Filter

直接標注在需要保護的 Action 上，僅對 `POST` / `PATCH` 生效：

```csharp
[HttpPost]
[IdempotencyKey]
public async Task<IActionResult> Create(CreateMemberRequest request, CancellationToken ct) { ... }

// 可選參數
[IdempotencyKey(
    TtlHours = 48,              // Key 保留時間，預設 24 小時
    LockTtlSeconds = 60,        // InProgress 鎖定時間，預設 30 秒
    Required = false,           // 允許不帶 header，預設 true
    ExcludeFields = ["nonce"]   // Fingerprint 計算時排除的欄位
)]
```

---

## API 使用方式

所有 `POST` 請求需帶 `Idempotency-Key` header（UUID v4 建議）：

```http
POST /api/members
Content-Type: application/json
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{ "name": "John Doe", "email": "john@example.com" }
```

### 回應狀態碼

| 狀態碼 | 情境 |
|--------|------|
| `201` | 首次請求成功 |
| `201` + `X-Idempotent-Replay: true` | 相同 Key 重播快取結果 |
| `400` | 缺少 `Idempotency-Key` header，或 Key 超過 255 字元 |
| `409` | 相同 Key 的請求正在處理中（InProgress） |
| `422` | 相同 Key 搭配不同的 Request Body（Fingerprint 不符） |

---

## 專案結構

```
src/
├── IdempotencyKeys/
│   ├── IIdempotencyKeyStore.cs       # 儲存層介面
│   ├── RedisIdempotencyKeyStore.cs   # Redis 實作（SET NX EX）
│   ├── EfIdempotencyKeyStore.cs      # PostgreSQL 實作（唯一約束）
│   ├── IdempotencyKeyAttribute.cs    # Action Filter（核心邏輯）
│   ├── IdempotencyKeyRecord.cs       # 狀態記錄 DTO
│   └── IdempotencyKeyStatus.cs       # InProgress / Completed / Failed
└── Members/                          # 範例 CRUD API（示範 Filter 套用）
docker-compose.yml                    # PostgreSQL + Redis
Taskfile.yml                          # 自動化測試腳本
```
