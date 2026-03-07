# Lab.Idempotent — Idempotency Key WebAPI

示範如何在 ASP.NET Core 10 WebAPI 使用 **HybridCache（L1 記憶體 + L2 Redis）** 與 **Redis 分散式鎖**實作 Idempotency Key，確保在多 Pod 環境下相同請求不會重複執行。

---

## 架構

```
Client
  │
  ├─ Pod 1 (port 8080)  ─┐
  │                       ├─ Redis（L2 Cache + 分散式鎖）
  └─ Pod 2 (port 8081)  ─┘
```

- **`[Idempotent]`** — Action Filter，攔截帶有 `Idempotency-Key` header 的請求
- **HybridCache** — L1（記憶體）+ L2（Redis）雙層快取，避免重複執行
- **Redis SETNX** — 分散式鎖，跨 Pod 並發保護

---

## 前置需求

| 工具 | 版本 |
|------|------|
| [.NET SDK](https://dotnet.microsoft.com/) | 10.0+ |
| [Docker](https://www.docker.com/) | 任意版本 |
| [Task](https://taskfile.dev/installation/) | 3.x |
| [Node.js](https://nodejs.org/) | 18+（用於 httpyac） |

---

## 快速開始

### 1. 安裝 httpyac CLI

```bash
task test-install
# 或直接：npm install -g httpyac
```

### 2. 一鍵執行所有測試

```bash
task e2e
```

執行流程：
1. 啟動 Redis + Pod1（8080）+ Pod2（8081）
2. 執行 `.http` 情境測試
3. 執行並發測試（驗證分散式鎖）
4. 關閉所有服務

---

## 可用指令

```bash
task up              # 啟動所有服務，等待兩個 Pod 就緒
task down            # 關閉所有服務
task test            # 執行 .http 情境測試（需先 task up）
task test-concurrent # 執行並發測試（需先 task up）
task e2e             # 完整流程：up → test → test-concurrent → down
task test-install    # 安裝 httpyac CLI
```

---

## 測試情境（`.http`）

檔案位置：`src/Lab.Idempotent.WebApi/Lab.Idempotent.WebApi.http`

| 情境 | 請求 | 預期結果 |
|------|------|---------|
| 1 | POST 無 `Idempotency-Key` header | `400 Bad Request` |
| 2 | POST 帶 key，第一次請求 | `200 OK`，執行 action 並快取 |
| 3 | POST 帶相同 key，重複請求 | `200 OK`，回傳**與情境 2 完全相同**的快取結果 |
| 4a | POST 帶新 key，body `{"temperatureC": 25}` | `200 OK` |
| 4b | POST 帶**相同** key，body `{"temperatureC": 99}` | `422 Unprocessable Entity`（payload 不一致） |
| 5 | POST 帶全新 key | `200 OK`，獨立執行 |
| 6 | GET 查詢所有資料 | `200 OK`，驗證資料只建立一次 |

---

## 並發測試（分散式鎖）

```bash
task test-concurrent
```

腳本（`scripts/test-concurrent.sh`）同時對兩個不同 Pod 送出相同 `Idempotency-Key` 的請求：

```
Pod1 (8080) ──→ SETNX ✅ 取得鎖 → 執行 action → 200
Pod2 (8081) ──→ SETNX ❌ 鎖被佔用              → 409 Conflict
```

預期輸出：

```
Testing concurrent requests with Idempotency-Key: concurrent-test-1234567890
Pod1 → http://localhost:8080  |  Pod2 → http://localhost:8081
Pod1 (8080): HTTP 200
Pod2 (8081): HTTP 409
✅ Concurrent test passed: one 200, one 409
```

> **為什麼需要兩個 Pod？**
> 單 Pod 內 HybridCache 有內建 stampede protection，相同 key 的並發請求只會執行一次 factory，不會觸發分散式鎖。409 只在兩個獨立 Pod 同時到達時才會觸發。

---

## HTTP 回應碼說明

| 狀態碼 | 說明 |
|--------|------|
| `200` | 成功（首次執行或快取命中） |
| `400` | 缺少 `Idempotency-Key` header |
| `409` | 相同 key 正在被另一個 Pod 處理中（並發衝突） |
| `422` | 相同 key 但 request payload 與原始請求不一致 |
