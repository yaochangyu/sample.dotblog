# Idempotency Key 最佳實踐

> 適用情境：Web API 多 Pod/Container 部署環境，使用 Redis 作為 Idempotency Key 儲存層。

## 目錄

- [1. 核心概念](#1-核心概念)
- [2. 標準規範](#2-標準規範ietf-draft)
- [3. 設計模式](#3-設計模式)
- [4. 多 Pod 競爭處理](#4-多-pod-競爭處理)
- [5. Redis 實作策略](#5-redis-實作策略)
- [6. 錯誤處理](#6-錯誤處理)
- [7. 客戶端重試策略](#7-客戶端重試策略)
- [8. Redis 故障策略](#8-redis-故障策略)
- [9. 安全性考量](#9-安全性考量)
- [10. 監控與可觀測性](#10-監控與可觀測性)
- [11. 業界參考實作](#11-業界參考實作)
- [12. 總結檢查清單](#12-總結檢查清單)

---

## 1. 核心概念

### 什麼是 Idempotency？

在數學與電腦科學中，**冪等操作（Idempotent Operation）** 是指執行一次與執行多次產生相同結果的操作。

### 為什麼需要 Idempotency Key？

在分散式系統中，以下情境會導致重複請求：

- 客戶端因網路超時而重試
- Load Balancer 將重試請求導向不同的 Pod
- 客戶端在傳輸過程中斷線，無法確認伺服器是否已處理

### HTTP 方法的冪等性

根據 [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110)：

| HTTP 方法 | 天生冪等？ | 需要 Idempotency Key？ |
|-----------|-----------|----------------------|
| GET       | ✅ 是     | ❌ 不需要             |
| HEAD      | ✅ 是     | ❌ 不需要             |
| PUT       | ✅ 是     | ❌ 不需要             |
| DELETE    | ✅ 是     | ❌ 不需要             |
| OPTIONS   | ✅ 是     | ❌ 不需要             |
| **POST**  | ❌ 否     | ✅ **需要**           |
| **PATCH** | ❌ 否     | ✅ **需要**           |

---

## 2. 標準規範（IETF Draft）

本節基於 [draft-ietf-httpapi-idempotency-key-header-07](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)（2025 年 10 月）。

### 2.1 Header 格式

```http
Idempotency-Key: "8e03978e-40d5-43e8-bc93-6894a57f9324"
```

- **Header 名稱**：`Idempotency-Key`
- **值格式**：Structured Header 的 String 類型（[RFC 8941](https://www.rfc-editor.org/rfc/rfc8941) Section 3.3.3）
- **建議**：使用 UUID v4 或具足夠熵值的隨機字串
- **長度限制**：建議不超過 255 字元（參考 Stripe 實作）

### 2.2 Idempotency Fingerprint

IETF 規範建議搭配 **Idempotency Fingerprint** 來確認請求唯一性，可採用：

- 整個 Request Payload 的 Checksum
- 選定欄位的 Checksum
- 欄位值逐一比對
- Request Digest / Signature

> **目的**：防止同一個 Idempotency Key 被用於不同的請求內容。

### 2.3 客戶端職責

- 為每個 **新的業務操作** 產生唯一的 Idempotency Key
- **重試時使用相同的 Key**
- 理解伺服器公布的冪等規範

### 2.4 伺服器職責

- 從 `Idempotency-Key` Header 取得 Key 值
- 產生 Fingerprint（如適用）
- 管理 Key 的生命週期（包含過期策略）
- 執行冪等邏輯（見下方流程）

---

## 3. 設計模式

### 3.1 請求處理流程

```
                        ┌─────────────────┐
                        │  收到 API 請求   │
                        └────────┬────────┘
                                 │
                        ┌────────▼────────┐
                        │  有 Idempotency  │──── 沒有 ──→ 回傳 400
                        │  Key Header？    │
                        └────────┬────────┘
                                 │ 有
                        ┌────────▼────────┐
                        │  執行 Redis      │
                        │  Lua Script      │
                        │  (原子操作)      │
                        └────────┬────────┘
                      ┌──────────┼──────────┐
                   ACQUIRED   PROCESSING  COMPLETED
                      │          │           │
             ┌────────▼──┐  ┌───▼────┐  ┌──▼──────┐
             │ 首次請求   │  │回 409  │  │回傳快取  │
             │ 執行業務   │  │Conflict│  │的回應    │
             │ 邏輯       │  └────────┘  └─────────┘
             └────────┬──┘
             ┌────────▼──┐
             │ 儲存結果   │
             │ 回傳回應   │
             └───────────┘
```

### 3.2 Key 的生命週期與狀態機

```
  ┌──────────┐     ┌──────────────┐     ┌───────────┐
  │ 不存在    │────▶│ processing   │────▶│ completed │
  └──────────┘     └──────┬───────┘     └───────────┘
                          │                    │
                          │                    │ TTL 到期
                          ▼                    ▼
                   ┌──────────────┐     ┌───────────┐
                   │ failed       │     │ 自動刪除   │
                   │ (可重試)     │     └───────────┘
                   └──────────────┘
```

**三種狀態**：

| 狀態         | 說明                         | 客戶端重試行為        |
|-------------|------------------------------|---------------------|
| `processing`| 請求正在處理中                 | 回傳 409 Conflict   |
| `completed` | 請求已完成，結果已快取         | 回傳快取的回應        |
| `failed`    | 請求失敗，允許重試             | 清除狀態，重新執行     |

### 3.3 Atomic Phase 模式（來自 Brandur / Stripe 經驗）

將請求生命週期拆分為 **原子階段（Atomic Phase）**：

1. **識別外部狀態變更（Foreign State Mutation）**：呼叫第三方 API、發送 Email 等
2. **每個外部變更獨立成一個階段**
3. **本地資料庫操作分組在外部變更之間**
4. **設定 Recovery Point**：每個階段完成後記錄檢查點

```
┌──────────┐   ┌──────────────┐   ┌──────────────┐   ┌──────────┐
│ Phase 1  │──▶│   Phase 2    │──▶│   Phase 3    │──▶│ Phase 4  │
│ 建立     │   │ 建立訂單     │   │ 呼叫外部     │   │ 更新狀態 │
│ Idemp Key│   │ 寫入 Audit   │   │ 支付 API     │   │ 發送通知 │
└──────────┘   └──────────────┘   └──────────────┘   └──────────┘
 recovery:      recovery:          recovery:          recovery:
 "started"      "order_created"    "payment_done"     "finished"
```

> **關鍵原則**：外部狀態變更一旦發生就無法回滾，必須在本地記錄後才發起呼叫。失敗時可從最近的 Recovery Point 繼續。

> ⚠️ **外部服務也需要冪等保護**：即使有 Recovery Point，若 Pod 在「呼叫外部 API 成功 → 更新 recovery_point」之間崩潰，重試時仍會重複呼叫外部 API。因此，**每個外部狀態變更本身也必須是冪等的**（例如：傳入 Stripe 自己的 idempotency key、使用具有唯一約束的交易 ID）。Recovery Point 解決的是「從哪裡繼續」，外部服務的冪等性解決的是「重複呼叫不會產生副作用」。

### 3.4 Atomic Phase 在 Redis 中的對應

Atomic Phase 的 Recovery Point 可直接對應 Redis Hash 的 `recovery_point` 欄位。
當請求涉及多個外部服務呼叫時，不應只用簡單的三態（processing/completed/failed），
而是將 `recovery_point` 記錄在 Redis Hash 中，讓重試時可從中斷點繼續：

```
Redis Hash 結構（含 Recovery Point）：

  status          "processing"
  recovery_point  "order_created"     ← 記錄目前到哪個階段
  request_hash    "sha256:abc123..."
  ...
```

**重試時的處理邏輯**：

```
收到重試請求
    │
    ├── status = "processing" 且 recovery_point = "started"
    │   └── 可能是前一個 Pod 崩潰 → 從頭執行
    │
    ├── status = "processing" 且 recovery_point = "order_created"
    │   └── 訂單已建立，跳過 Phase 2，從 Phase 3 繼續
    │
    └── status = "completed"
        └── 回傳快取結果
```

> **適用時機**：當一個 API 請求包含多個外部狀態變更（如：先建立訂單、再呼叫支付、再發通知）時，才需要 Recovery Point。若業務邏輯只有單一操作，三態模型已足夠。

---

## 4. 多 Pod 競爭處理

### 4.1 核心挑戰

在多 Pod 部署中，**同一個 Idempotency Key 的重試請求可能被路由到不同的 Pod**：

```
Client ──重試──▶ Load Balancer ─┬──▶ Pod A（正在處理原始請求）
                                └──▶ Pod B（收到重試請求）
```

### 4.2 使用 Redis 實現分散式互斥

解決多 Pod 競爭的關鍵是 **Redis 的原子操作語意**。
透過 Lua Script（內部使用 `HGET` + `HSET` + `EXPIRE` 的原子組合），
確保「檢查狀態 → 判斷 → 寫入」在同一個 Redis 執行緒中完成，不會被其他 Pod 打斷：

```
時間軸 ──────────────────────────────────────────────▶

Pod A:  Lua Script → ACQUIRED ✅ → 執行業務邏輯 → 儲存結果
Pod B:       Lua Script → PROCESSING ❌ → 回傳 409 Conflict
Pod C:            Lua Script → PROCESSING ❌ → 回傳 409 Conflict
```

> **為什麼不用裸的 `SET NX EX`？**
>
> `SET NX EX` 只能設定一個簡單的 String 值。但冪等機制需要儲存多個欄位
>（status、request_hash、response 等），必須使用 Hash 結構。
> 若將 `SET NX`（鎖）與 `HSET`（資料）拆成兩步，中間存在競爭窗口。
> Lua Script 將所有操作合併為單一原子操作，詳見 [Section 5.3](#53-使用-lua-script-保證原子性推薦做法)。

### 4.3 Redis 作為共享狀態的優勢

| 特性            | 說明                                            |
|----------------|------------------------------------------------|
| 原子操作        | Lua Script 在 Redis 中以單執行緒執行，天然互斥    |
| 跨 Pod 可見     | 所有 Pod 共享同一個 Redis，狀態即時同步            |
| 自動過期        | TTL 機制防止因 Pod 崩潰導致的死鎖                 |
| 高效能          | 記憶體操作，延遲通常在微秒級                      |

### 4.4 防止死鎖：Processing Timeout

若 Pod 在處理過程中崩潰，需要有機制讓其他 Pod 能接手：

```
策略：設定 processing 狀態的 TTL 短於 completed 狀態

1. processing 狀態 TTL = 30~60 秒（依業務邏輯最大執行時間而定）
2. completed 狀態 TTL = 24 小時（參考 Stripe 做法）
3. Pod 崩潰後，processing 狀態自動過期
4. 下次重試可以重新取得執行權
```

---

## 5. Redis 實作策略

### 5.1 Key 的命名規範

所有 Redis Key 統一使用以下格式，貫穿整份文件：

```
格式：idempotency:{user_id}:{method}:{path}:{idempotency_key}

範例：idempotency:user-42:POST:/api/orders:8e03978e-40d5-43e8-bc93-6894a57f9324
```

**設計理由**：

| 組成部分           | 用途                                         |
|-------------------|----------------------------------------------|
| `idempotency`     | 命名空間前綴，區隔其他 Redis Key               |
| `{user_id}`       | 綁定使用者身份，防止跨使用者存取（見安全性章節）   |
| `{method}:{path}` | 綁定 endpoint，同一 Key 在不同 endpoint 不衝突  |
| `{idempotency_key}` | 客戶端提供的唯一識別碼                        |

### 5.2 Value 的資料結構

使用 Redis Hash 儲存完整的冪等狀態：

```redis
HSET idempotency:{user_id}:{method}:{path}:{key}
  status          "processing"          # processing | completed | failed
  recovery_point  "started"             # 目前的 Recovery Point（見 Section 3.4）
  request_hash    "sha256:abc123..."    # Request Body 的 Fingerprint
  response_code   ""                    # HTTP Status Code（completed 後填入）
  response_headers ""                   # 需快取的 Response Headers（JSON 格式）
  response_body   ""                    # Response Body（completed 後填入）
  pod_id          "pod-abc-123"         # 處理此請求的 Pod 識別碼
  created_at      "1709827200"          # Unix Timestamp
```

> **關於 `response_headers`**：某些回應的重要資訊在 Header 中而非 Body（如 `201 Created` 的 `Location` header）。快取回應時應一併儲存需重播的 Headers，以 JSON 格式存放：
> ```json
> {"Location": "/api/orders/123", "X-Request-Id": "abc-456"}
> ```
> 不需要快取所有 Headers，僅快取對客戶端有意義的即可（如 `Location`、自訂 Headers）。

#### Response Body 大小考量

直接將完整 Response Body 存入 Redis 可能導致記憶體壓力，需依回應大小選擇策略：

| 回應大小     | 策略                                                        |
|-------------|-------------------------------------------------------------|
| < 1 KB      | 直接存入 `response_body` 欄位                                |
| 1 KB ~ 1 MB | 存入 Redis，但設定合理的 `maxmemory-policy`                   |
| > 1 MB      | 僅存 `response_code`，回應 body 改存外部儲存（如 S3、Blob），在 Redis 中存放參考指標 |

> **建議**：對大多數 API 來說，回應通常在幾 KB 以內，直接存 Redis 即可。但若 API 會回傳大型資料集（如檔案下載、報表），應改用參考指標模式。

### 5.3 使用 Lua Script 保證原子性（推薦做法）

> ⚠️ **為什麼不用多步驟 Redis 指令？**
>
> 若將「讀取狀態 → 判斷 → 寫入」拆成多條 Redis 指令，即使第一步用了 `SET NX`，
> 在第一步與第二步之間仍存在時間窗口，其他 Pod 可能讀到尚未初始化的 Hash。
> **必須使用 Lua Script 將整個流程合併為單一原子操作。**

#### 取得執行權的 Lua Script

```lua
-- idempotency_acquire.lua
-- KEYS[1] = idempotency:{user_id}:{method}:{path}:{idempotency_key}
-- ARGV[1] = pod_id
-- ARGV[2] = request_hash
-- ARGV[3] = processing_ttl (秒)

local status = redis.call('HGET', KEYS[1], 'status')

if status == false then
    -- Key 不存在：首次請求，建立並取得執行權
    redis.call('HSET', KEYS[1],
        'status', 'processing',
        'request_hash', ARGV[2],
        'pod_id', ARGV[1],
        'created_at', tostring(redis.call('TIME')[1]))
    redis.call('EXPIRE', KEYS[1], tonumber(ARGV[3]))
    return cjson.encode({ result = 'ACQUIRED' })
end

-- 比對 Fingerprint
local stored_hash = redis.call('HGET', KEYS[1], 'request_hash')
if stored_hash ~= ARGV[2] then
    return cjson.encode({ result = 'FINGERPRINT_MISMATCH' })
end

if status == 'processing' then
    return cjson.encode({ result = 'PROCESSING' })

elseif status == 'completed' then
    local code = redis.call('HGET', KEYS[1], 'response_code')
    local headers = redis.call('HGET', KEYS[1], 'response_headers')
    local body = redis.call('HGET', KEYS[1], 'response_body')
    return cjson.encode({
        result = 'COMPLETED',
        response_code = code,
        response_headers = headers,
        response_body = body
    })

elseif status == 'failed' then
    -- 前次失敗：重置為 processing，讓此請求重新執行
    redis.call('HSET', KEYS[1],
        'status', 'processing',
        'pod_id', ARGV[1],
        'created_at', tostring(redis.call('TIME')[1]))
    redis.call('EXPIRE', KEYS[1], tonumber(ARGV[3]))
    return cjson.encode({ result = 'ACQUIRED' })
end

return cjson.encode({ result = 'PROCESSING' })
```

**回傳值說明**（JSON 格式，避免分隔符衝突）：

| `result` 值            | 說明                         | 伺服器動作            |
|------------------------|------------------------------|----------------------|
| `ACQUIRED`             | 首次請求，已取得執行權         | 執行業務邏輯          |
| `PROCESSING`           | 另一個 Pod 正在處理           | 回傳 409 Conflict    |
| `COMPLETED`            | 已完成，含快取的回應           | 回傳快取結果          |
| `FINGERPRINT_MISMATCH` | Key 已被不同請求內容使用       | 回傳 422             |

#### 完成業務邏輯後的 Lua Script

```lua
-- idempotency_complete.lua
-- KEYS[1] = idempotency:{user_id}:{method}:{path}:{idempotency_key}
-- ARGV[1] = pod_id（用於驗證擁有權）
-- ARGV[2] = response_code
-- ARGV[3] = response_headers (JSON)
-- ARGV[4] = response_body
-- ARGV[5] = completed_ttl (秒)

local current_pod = redis.call('HGET', KEYS[1], 'pod_id')
if current_pod ~= ARGV[1] then
    return cjson.encode({ result = 'NOT_OWNER' })
end

redis.call('HSET', KEYS[1],
    'status', 'completed',
    'response_code', ARGV[2],
    'response_headers', ARGV[3],
    'response_body', ARGV[4])
redis.call('EXPIRE', KEYS[1], tonumber(ARGV[5]))

return cjson.encode({ result = 'OK' })
```

#### 標記失敗的 Lua Script

```lua
-- idempotency_fail.lua
-- KEYS[1] = idempotency:{user_id}:{method}:{path}:{idempotency_key}
-- ARGV[1] = pod_id（用於驗證擁有權）
-- ARGV[2] = failed_ttl (秒)

local current_pod = redis.call('HGET', KEYS[1], 'pod_id')
if current_pod ~= ARGV[1] then
    return cjson.encode({ result = 'NOT_OWNER' })
end

redis.call('HSET', KEYS[1], 'status', 'failed')
redis.call('EXPIRE', KEYS[1], tonumber(ARGV[2]))

return cjson.encode({ result = 'OK' })
```

### 5.4 TTL 策略建議

| 狀態         | 建議 TTL       | 說明                                     |
|-------------|---------------|------------------------------------------|
| `processing`| 30 ~ 60 秒    | 依 API 最大執行時間決定，防止崩潰後死鎖       |
| `completed` | 24 小時       | 參考 Stripe 做法，超過此時間重試視為新請求     |
| `failed`    | 5 ~ 10 分鐘   | 給客戶端足夠的重試窗口                      |

---

## 6. 錯誤處理

### 6.1 標準錯誤回應（遵循 IETF Draft + RFC 7807）

#### 缺少 Idempotency-Key Header

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://api.example.com/docs/idempotency",
  "title": "Idempotency-Key is missing",
  "detail": "此操作為冪等操作，必須提供 Idempotency-Key Header。",
  "status": 400
}
```

#### 同一 Key 使用不同的 Request Payload

```http
HTTP/1.1 422 Unprocessable Content
Content-Type: application/problem+json

{
  "type": "https://api.example.com/docs/idempotency",
  "title": "Idempotency-Key is already used",
  "detail": "此 Idempotency-Key 已被用於不同的請求內容，不可重複使用。",
  "status": 422
}
```

#### 請求正在被其他 Pod 處理中

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://api.example.com/docs/idempotency",
  "title": "A request is outstanding for this Idempotency-Key",
  "detail": "相同 Idempotency-Key 的請求正在處理中，請稍後重試。",
  "status": 409
}
```

### 6.2 不同錯誤類型的冪等行為

| 錯誤類型                | 是否儲存結果？ | 客戶端重試？ | 說明                           |
|------------------------|-------------|------------|-------------------------------|
| 參數驗證失敗 (400)       | ❌ 否       | 修正後重試   | 請求尚未進入業務邏輯             |
| 認證失敗 (401/403)      | ❌ 否       | 修正後重試   | 與冪等邏輯無關                  |
| Fingerprint 不符 (422)  | ❌ 否       | 使用新 Key  | 同一 Key 不可用於不同請求        |
| 併發衝突 (409)          | ❌ 否       | 原 Key 重試 | 等前一個請求完成後自動回傳結果    |
| 業務邏輯失敗 (4xx)       | ✅ 是       | 使用新 Key  | 確定性錯誤，重試無意義           |
| 伺服器內部錯誤 (500)     | ⚠️ 視情況   | 原 Key 重試 | 暫時性錯誤可設為 failed 允許重試  |
| 外部服務超時            | ❌ 否       | 原 Key 重試 | 設為 failed，允許重試            |

### 6.3 失敗時的清理策略

```
業務邏輯拋出例外
    │
    ├── 確定性錯誤（如：餘額不足）
    │   └── 儲存錯誤回應為 completed → 後續重試回傳相同錯誤
    │
    ├── 暫時性錯誤（如：資料庫超時）
    │   └── 標記為 failed → 允許客戶端用同一 Key 重試
    │
    └── Pod 崩潰
        └── processing TTL 到期 → Key 自動清除 → 允許重試
```

---

## 7. 客戶端重試策略

### 7.1 重試時機與行為

| 收到的回應               | 客戶端行為                                           |
|-------------------------|------------------------------------------------------|
| 網路超時 / 無回應         | 使用**相同** Idempotency Key 重試                     |
| `409 Conflict`          | 使用**相同** Key，等待後重試（代表前一個請求還在處理中）   |
| `2xx` 成功              | 不重試                                                |
| `400` / `401` / `403`   | **不要重試**，修正請求內容或認證後使用**新 Key**          |
| `422` Fingerprint 不符   | 使用**新 Key** 發送請求                                |
| `500` / `502` / `503`   | 使用**相同 Key** 重試（伺服器端會標記為 failed 允許重試）  |

### 7.2 Exponential Backoff with Jitter

收到 `409 Conflict` 時，客戶端應使用指數退避搭配抖動（Jitter）避免重試風暴：

```
retry_delay = min(base_delay × 2^attempt + random_jitter, max_delay)

範例：
  第 1 次重試：200ms + random(0~100ms) = 200~300ms
  第 2 次重試：400ms + random(0~100ms) = 400~500ms
  第 3 次重試：800ms + random(0~100ms) = 800~900ms
  ...
  上限：不超過 30 秒
```

### 7.3 最大重試次數

建議設定上限（如 3~5 次），超過後應向使用者回報錯誤，避免無限重試。

---

## 8. Redis 故障策略

### 8.1 Fail-Open vs Fail-Closed

當 Redis 無法連線時，系統必須做出選擇：

| 策略         | 行為                          | 適用場景                        |
|-------------|-------------------------------|-------------------------------|
| **Fail-Open**  | Redis 不可用時，跳過冪等檢查，直接處理請求 | 可用性優先（如一般商品頁面操作） |
| **Fail-Closed**| Redis 不可用時，拒絕請求，回傳 `503`       | 正確性優先（如支付、轉帳操作）   |

**建議**：涉及金錢或不可逆操作的 endpoint 應使用 **Fail-Closed**，其餘可依業務需求選擇。

### 8.2 Redis 高可用部署

| 部署模式          | 說明                                             | 注意事項                                   |
|------------------|--------------------------------------------------|-------------------------------------------|
| Redis Sentinel   | 主從架構 + 自動故障轉移                            | 故障轉移期間可能有短暫的不可用               |
| Redis Cluster    | 分散式，資料分片到多個節點                          | Lua Script 的 KEYS 必須落在同一個 hash slot |
| 單節點 + 持久化   | 最簡單，適合非關鍵場景                              | 重啟時會遺失 processing 狀態                |

### 8.3 Redis Cluster 與 Lua Script 的相容性

Redis Cluster 模式下，Lua Script 中所有 `KEYS` 必須位於同一個 hash slot。
由於本文件的 Lua Script **僅使用單一 `KEYS[1]`**，天然相容 Redis Cluster，
不需要額外的 Hash Tag 處理。

> **何時需要 Hash Tag？** 若未來擴展為多 Key 設計（例如將 lock 和 data 分開存放），
> 才需要使用 Hash Tag（如 `{user-42:POST:/api/orders}:lock`）確保多個 Key 落在同一 slot。
> 目前的單 Key Hash 設計不需要。

---

## 9. 安全性考量

### 9.1 防止注入攻擊

- **驗證 Key 格式**：限制為 UUID 格式或特定字元集
- **限制 Key 長度**：最長 255 字元
- **不要將 Key 直接用於資料庫查詢的拼接**

### 9.2 防止資料洩漏

- **複合 Key 已內建防護**：本文件統一使用 `idempotency:{user_id}:{method}:{path}:{key}` 格式，
  已將使用者身份綁定在 Redis Key 中。即使攻擊者猜到其他人的 Idempotency Key，
  也因 `user_id` 不同而無法取得快取結果。

- **使用高熵值 Key**：UUID v4 提供 122 bits 的隨機性，難以暴力猜測

### 9.3 防止 Key 濫用

- **Rate Limiting**：限制單一使用者的 Idempotency Key 建立頻率
- **限定 Key 的作用範圍**：每個 Key 僅對特定 endpoint 有效

---

## 10. 監控與可觀測性

### 10.1 關鍵指標（Metrics）

| 指標名稱                              | 類型      | 說明                                        |
|--------------------------------------|-----------|---------------------------------------------|
| `idempotency_requests_total`         | Counter   | 冪等請求總數（含首次與重試）                    |
| `idempotency_cache_hit_total`        | Counter   | 命中快取（completed 狀態）的次數                |
| `idempotency_conflict_total`         | Counter   | 回傳 409 Conflict 的次數                      |
| `idempotency_fingerprint_mismatch`   | Counter   | Fingerprint 不符（422）的次數                  |
| `idempotency_processing_timeout`     | Counter   | processing 狀態因 TTL 到期而自動清除的次數      |
| `idempotency_redis_error_total`      | Counter   | Redis 操作失敗次數                             |
| `idempotency_acquire_duration_ms`    | Histogram | 取得執行權（Lua Script）的延遲分佈              |

### 10.2 告警建議

| 條件                                     | 嚴重性 | 可能原因                              |
|-----------------------------------------|--------|---------------------------------------|
| `conflict_total` 突然大幅上升             | Warning | 客戶端重試過於頻繁，或業務邏輯執行時間過長 |
| `processing_timeout` 持續出現             | Error   | Pod 頻繁崩潰，或 processing TTL 設定過短  |
| `redis_error_total` > 0                  | Error   | Redis 連線問題，需檢查網路或 Redis 健康狀態 |
| `fingerprint_mismatch` 持續上升           | Warning | 客戶端可能誤用 Idempotency Key            |
| `cache_hit / requests` 比例異常高         | Info    | 客戶端可能有重複送出的 Bug                 |

### 10.3 日誌建議

每次冪等操作應記錄結構化日誌，包含：

```json
{
  "event": "idempotency_check",
  "idempotency_key": "8e03978e-...",
  "endpoint": "POST /api/orders",
  "result": "ACQUIRED | PROCESSING | COMPLETED | FINGERPRINT_MISMATCH",
  "pod_id": "pod-abc-123",
  "duration_ms": 2
}
```

---

## 11. 業界參考實作

以下組織使用 `Idempotency-Key` Header（來自 IETF Draft 實作狀態）：

| 組織          | Header 名稱                 | 參考連結                                           |
|--------------|-----------------------------|----------------------------------------------------|
| **Stripe**   | `Idempotency-Key`          | https://stripe.com/docs/api/idempotent_requests    |
| **Adyen**    | `Idempotency-Key`          | https://docs.adyen.com/development-resources/api-idempotency/ |
| **PayPal**   | `PayPal-Request-Id`        | https://developer.paypal.com/docs/business/develop/idempotency |
| **Square**   | `idempotency_key`（Body）   | https://developer.squareup.com/docs/build-basics/using-rest-api |
| **Google**   | `requestId`（Body）         | https://developers.google.com/standard-payments/   |

### Stripe 的關鍵設計決策

- Key 最長 255 字元
- TTL 為 24 小時，超過後自動清除
- 比對請求參數，不同參數使用同一 Key 會回傳錯誤
- 僅對 `POST` 請求啟用冪等
- 驗證失敗與併發衝突不儲存結果，可重試

---

## 12. 總結檢查清單

### 設計階段

- [ ] 決定哪些 endpoint 需要冪等保護（通常是所有非冪等的寫入操作）
- [ ] 定義 Idempotency Key 的格式規範（建議 UUID v4）
- [ ] 定義 Key 的作用範圍（per user + per endpoint）
- [ ] 定義 TTL 策略（processing / completed / failed）
- [ ] 定義 Fingerprint 演算法（Request Body Hash）
- [ ] 決定 Redis 故障時的策略（Fail-Open 或 Fail-Closed）

### 實作階段

- [ ] 使用 Redis Lua Script 實現原子性的取得 / 完成 / 失敗操作
- [ ] 統一 Redis Key 格式：`idempotency:{user_id}:{method}:{path}:{key}`
- [ ] 實作三種狀態（processing / completed / failed）的轉換邏輯
- [ ] 區分確定性錯誤與暫時性錯誤的處理策略
- [ ] 使用 [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) Problem Details 格式回傳錯誤
- [ ] 評估 Response Body 大小，選擇適當的快取策略

### 客戶端

- [ ] 實作 Exponential Backoff with Jitter 重試策略
- [ ] 設定最大重試次數（建議 3~5 次）
- [ ] 依回應狀態碼決定使用相同 Key 或新 Key 重試

### 多 Pod 部署

- [ ] 確認所有 Pod 連接到同一個 Redis 叢集
- [ ] processing 狀態的 TTL 必須大於 API 最長執行時間
- [ ] Pod 崩潰後，processing 狀態會自動過期，不會造成死鎖
- [ ] 規劃 Redis 高可用架構（Sentinel 或 Cluster）
- [ ] 若使用 Redis Cluster，確認 Lua Script 的 KEYS 使用 Hash Tag

### 監控

- [ ] 設定冪等相關的 Metrics（cache hit、conflict、timeout）
- [ ] 設定告警規則（Redis 錯誤、processing timeout 頻繁發生）
- [ ] 記錄結構化日誌，包含 Idempotency Key 與操作結果

### 安全性

- [ ] 驗證 Idempotency Key 的格式與長度
- [ ] Redis Key 已包含 User ID，防止跨使用者存取
- [ ] 搭配 Rate Limiting 防止 Key 濫用

---

## 參考資料

- [IETF Draft: The Idempotency-Key HTTP Header Field (draft-07)](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)
- [RFC 9110: HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110)
- [RFC 7807: Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807)
- [Stripe: Idempotent Requests](https://stripe.com/docs/api/idempotent_requests)
- [Brandur: Implementing Stripe-like Idempotency Keys in Postgres](https://brandur.org/idempotency-keys)
- [Redis: Distributed Locks](https://redis.io/docs/latest/develop/use/patterns/distributed-locks/)
