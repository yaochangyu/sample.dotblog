# Idempotency Key 最佳實踐：設計指南與架構決策參考

## 目錄

- [1. 什麼是 Idempotency Key](#1-什麼是-idempotency-key)
- [2. 標準規範](#2-標準規範)
- [3. 核心設計原則](#3-核心設計原則)
- [4. 完整請求處理流程](#4-完整請求處理流程)
- [5. 狀態機模型](#5-狀態機模型)
- [6. 多 Pod / 多容器環境的併發控制](#6-多-pod--多容器環境的併發控制)
- [7. 儲存方案比較](#7-儲存方案比較)
- [8. Request Fingerprint 驗證](#8-request-fingerprint-驗證)
- [9. 錯誤處理策略](#9-錯誤處理策略)
- [10. TTL 與過期策略](#10-ttl-與過期策略)
- [11. 基礎設施故障的降級策略](#11-基礎設施故障的降級策略)
- [12. 可觀測性](#12-可觀測性)
- [13. 常見反模式](#13-常見反模式)
- [14. 業界參考實作](#14-業界參考實作)
- [15. 架構決策清單](#15-架構決策清單)
- [Sources](#sources)

---

## 1. 什麼是 Idempotency Key

Idempotency Key 是由**客戶端產生的唯一識別值**，隨請求一起傳送至伺服器。伺服器透過此值辨識重複請求，確保同一操作無論被執行幾次，結果都與只執行一次相同。

### 為什麼需要

在分散式系統中，網路不可靠是常態。客戶端可能因為：

- 網路超時而重試
- 負載均衡器重新路由
- 客戶端 crash 後重啟重送

若沒有冪等機制，這些重試可能導致**重複扣款、重複建立訂單**等嚴重問題。

### 適用場景

| HTTP 方法 | 天生冪等？ | 是否需要 Idempotency Key |
|-----------|-----------|------------------------|
| GET       | 是        | 不需要                  |
| PUT       | 是        | 通常不需要              |
| DELETE    | 是        | 通常不需要              |
| **POST**  | **否**    | **需要**                |
| **PATCH** | **否**    | **視情況需要**          |

> 重點：Idempotency Key 主要用於**非冪等的寫入操作**（POST / PATCH）。

---

## 2. 標準規範

### IETF Draft: Idempotency-Key HTTP Header Field

IETF HTTPAPI 工作組正在制定標準草案（[draft-ietf-httpapi-idempotency-key-header-07](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)），目前仍為草案階段。

**規範重點：**

- Header 名稱：`Idempotency-Key`
- 值格式：Structured Header 的 String 類型
- 值必須唯一，且不得與不同 request payload 重複使用
- 建議使用 UUID v4 或具足夠 entropy 的隨機字串

**Header 範例：**

```
Idempotency-Key: "8e03978e-40d5-43e8-bc93-6894a57f9324"
```

---

## 3. 核心設計原則

### 3.1 原子性（Atomicity）

**Key 的檢查與業務邏輯的執行必須是原子操作。**

這是最關鍵的原則。如果「檢查 key 是否存在」和「執行業務邏輯」不在同一個原子操作中，兩個並發請求都可能通過「尚未處理」的檢查，導致重複執行。

```
錯誤做法：
1. 檢查 key 是否存在     ← 兩個請求都可能在這裡通過
2. 執行業務邏輯
3. 儲存 key

正確做法：
1. 以原子操作嘗試取得鎖（INSERT ... ON CONFLICT / SET NX）
2. 只有取得鎖的請求執行業務邏輯
3. 在同一交易中儲存結果
```

> **注意：原子性的邊界限制。** 上述「在同一交易中儲存結果」僅適用於業務邏輯完全在資料庫內完成的場景。若業務邏輯涉及**外部服務呼叫**（如金流 API、第三方通知），外部呼叫無法被包進 DB transaction 中，原子性無法靠單一交易達成。此時需採用更進階的設計，例如 §4 中介紹的 Atomic Phases 模型。

### 3.2 完整回應快取

儲存首次請求的**完整回應**（包含 status code 和 response body），後續重試直接回傳相同結果。

### 3.3 回應標記

透過 response header 標記此回應為快取重播：

```
X-Idempotent-Replay: true
```

讓客戶端能區分「首次處理的結果」與「重播的快取結果」。

### 3.4 客戶端職責

- 由客戶端產生 Idempotency Key（建議 UUID v4）
- 同一個業務操作的重試，必須使用**相同的 key**
- 不同的業務操作，必須使用**不同的 key**

---

## 4. 完整請求處理流程

以下是一個 Idempotency Key 從請求進入到回應的完整端對端流程，整合了鎖、狀態機、fingerprint 驗證與錯誤處理的所有分支：

```
Client 發送請求（帶 Idempotency-Key header）
│
▼
[1] 參數驗證（格式、權限等）
│
├─ 驗證失敗 → 回傳 4xx，不記錄 key，結束
│
▼
[2] 以原子操作嘗試建立 key（SET NX / INSERT ... ON CONFLICT）
│
├─ key 已存在 → [3] 讀取現有 key 狀態
│                   │
│                   ├─ IN_PROGRESS → 回傳 409 Conflict，結束
│                   │
│                   ├─ COMPLETED
│                   │     │
│                   │     ▼
│                   │   [4] 驗證 request fingerprint
│                   │     │
│                   │     ├─ 不匹配 → 回傳 422（key 已被不同請求使用），結束
│                   │     │
│                   │     └─ 匹配 → 回傳快取回應
│                   │               + X-Idempotent-Replay: true，結束
│                   │
│                   └─ FAILED
│                         │
│                         ├─ 確定性失敗 → 回傳快取的錯誤回應，結束
│                         └─ 暫時性失敗 → 允許重新取得鎖，進入 [5]
│
▼
[5] 取得鎖成功（key 狀態 = IN_PROGRESS）
│
▼
[6] 儲存 request fingerprint
│
▼
[7] 執行業務邏輯
│
├─ 成功 → 狀態設為 COMPLETED，快取回應，回傳結果，結束
│
├─ 確定性失敗（如餘額不足且已扣款）
│     → 狀態設為 FAILED，快取錯誤回應，回傳結果，結束
│
└─ 暫時性失敗（如下游服務超時）
      → 刪除 key（或標記為可重試），回傳 5xx，結束
```

> 這張流程圖涵蓋了本文件後續各章節的核心概念。建議先掌握此全貌，再深入各章節細節。

---

## 5. 狀態機模型

Idempotency Key 在伺服器端是一個有狀態的實體，建議以**狀態機**建模：

```
          首次請求到達
               │
               ▼
        ┌──────────────┐
        │  IN_PROGRESS  │ ◄── 寫入 key，取得鎖
        └──────┬───────┘
               │
         業務邏輯執行
               │
        ┌──────┴───────┐
        ▼              ▼
 ┌────────────┐  ┌──────────┐
 │ COMPLETED  │  │  FAILED  │
 │ (快取回應)  │  │ (見 §9)  │
 └────────────┘  └──────────┘
```

### 各狀態的行為

| 狀態 | 含義 | 收到重複請求時的行為 |
|------|------|-------------------|
| `IN_PROGRESS` | 首次請求正在處理中 | 回傳 `409 Conflict` 或等待完成後回傳結果 |
| `COMPLETED` | 已成功處理，回應已快取 | 直接回傳快取的回應 |
| `FAILED` | 處理失敗（見[錯誤處理策略](#9-錯誤處理策略)） | 依錯誤類型決定是否允許重試 |

### 進階：Brandur 的 Atomic Phases 模型

> **何時需要？** 上面的三狀態模型適用於大多數場景——業務邏輯可以在單一操作中完成。但當業務邏輯涉及**多個步驟且包含外部服務呼叫**（如：建立訂單 → 呼叫金流 API 扣款 → 發送通知），單一交易無法涵蓋外部呼叫，三狀態模型就不夠用了。此時需要 Atomic Phases。

Stripe 工程師 Brandur 提出更細緻的實作方式：將 API 請求拆成多個 **atomic phase**，每個 phase 都是一個資料庫交易，phase 之間以 **recovery point** 串接。

```
Recovery Points: started → ride_created → charge_created → finished

Phase 1 (tx1)          Phase 2 (tx2)           Phase 3 (tx3)          Phase 4 (tx4)
建立 idempotency key → 建立 ride 記錄      → 呼叫 Stripe 扣款     → 更新最終狀態
recovery: started      recovery: ride_created  recovery: charge_created  recovery: finished
```

若請求在任何階段中斷，恢復時可從最後一個 recovery point 繼續，而非從頭開始。

**選擇指引：**

| 場景 | 建議模型 |
|------|---------|
| 業務邏輯在單一 DB 交易中完成 | 三狀態模型（IN_PROGRESS / COMPLETED / FAILED） |
| 業務邏輯涉及外部 API 呼叫或多步驟流程 | Atomic Phases + Recovery Points |

---

## 6. 多 Pod / 多容器環境的併發控制

> 這是本文件的重點場景：同一個 API 端點由多個 Pod/Container 提供服務。

### 問題

當請求經過 Load Balancer 分發到不同 Pod 時，同一個 Idempotency Key 的重複請求可能被路由到不同 Pod。**應用程式層級的記憶體鎖在此場景完全無效。**

### 解法：分散式鎖 + 共享儲存

```
Client ──► Load Balancer ──┬──► Pod A ──┐
                           │            ├──► 共享儲存 (Redis / DB)
                           └──► Pod B ──┘
```

**關鍵要求：**

1. **鎖必須是分散式的**：所有 Pod 看到同一把鎖
2. **儲存必須是共享的**：所有 Pod 讀寫同一份 idempotency key 狀態
3. **鎖必須有 TTL**：防止 Pod crash 後鎖永遠無法釋放（死鎖）

### 併發請求的處理策略

當第二個請求到達而第一個請求仍在處理時，有兩種策略：

| 策略 | 做法 | 優點 | 缺點 |
|------|------|------|------|
| **拒絕（Reject）** | 回傳 `409 Conflict` | 實作簡單、無等待開銷 | 客戶端需自行重試 |
| **等待（Wait）** | 輪詢/長輪詢直到首次請求完成 | 客戶端體驗較好 | 實作複雜、佔用連線資源 |

> **建議**：多數場景下採用 **Reject 策略**（回傳 409）更為穩健，讓客戶端自行決定重試策略。

---

## 7. 儲存方案比較

### 方案總覽

| 面向 | Redis | 關聯式資料庫 (RDBMS) | Redis + RDBMS 混合 |
|------|-------|---------------------|-------------------|
| **延遲** | 極低（< 1ms） | 較高（數 ms） | 鎖快、持久化穩 |
| **持久性** | 預設不持久（可設定 AOF/RDB） | 完全持久 | 混合 |
| **原子性** | SET NX + Lua script | DB Transaction + UNIQUE constraint | 分層處理 |
| **跨 Pod** | 天然支援（共享 Redis） | 天然支援（共享 DB） | 天然支援 |
| **TTL 支援** | 原生支援 | 需自行實作清理機制 | Redis 原生 TTL |
| **一致性** | 弱一致（非同步複寫時） | 強一致 | 依設計而定 |
| **運維成本** | 需維護 Redis 叢集 | 通常已有 DB 基礎設施 | 兩者皆需維護 |

### 方案 A：Redis

**適用場景**：高吞吐量、可接受極端情況下的少量重複

```
流程：
1. SET idempotency:{key} IN_PROGRESS NX EX 300   ← 原子取得鎖，TTL 300 秒
2. 若設定成功 → 執行業務邏輯 → SET idempotency:{key} {response} EX 86400
3. 若設定失敗 → 檢查狀態 → 回傳快取結果或 409
```

**優點：**
- 極低延遲
- 原生 TTL 管理
- SET NX 提供原子操作

**風險：**
- Redis 主從切換時可能遺失已寫入的 key（腦裂問題）
- 若 Redis crash，已完成但尚未持久化的 key 會遺失

**緩解措施：**
- 使用 Redis Sentinel 或 Redis Cluster 提升可用性
- 對於金融等關鍵場景，搭配資料庫作為最終一致的備份

### 方案 B：關聯式資料庫

**適用場景**：強一致性需求、已有 DB 基礎設施、業務邏輯與 key 可在同一交易中處理

```
流程：
1. BEGIN TRANSACTION
2. INSERT INTO idempotency_keys (key, status) VALUES (?, 'IN_PROGRESS')
   ← UNIQUE constraint 確保原子性
3. 執行業務邏輯
4. UPDATE idempotency_keys SET status='COMPLETED', response=? WHERE key=?
5. COMMIT
```

**優點：**
- 強一致性，不怕遺失
- Key 狀態與業務資料可在同一交易中保證原子性（最大優勢）
- 不需額外基礎設施

**風險：**
- 延遲較高
- 高併發下可能產生鎖競爭
- 需自行實作 TTL 清理機制（排程清理過期 key）

### 方案 C：Redis + RDBMS 混合

**適用場景**：需要高效能的併發控制，又需要強一致的持久化

```
流程：
1. Redis SET NX 取得分散式鎖（快速擋住並發）
2. DB Transaction 執行業務邏輯並儲存 idempotency key 狀態（強一致）
3. Redis 釋放鎖，並快取回應結果（加速後續重試查詢）
```

**優點：**
- 兼顧效能與一致性
- Redis 負責快速的併發控制
- DB 負責持久化與強一致性保證

**風險：**
- 架構複雜度較高
- 需處理 Redis 與 DB 之間的狀態不一致
- 運維成本高（需維護兩套基礎設施）

### 分散式鎖的選擇

| 方案 | 說明 | 適用場景 |
|------|------|---------|
| **Redis SET NX** | 單一 Redis 節點的原子鎖 | 效能優先，可接受極端 edge case |
| **Redlock** | 跨 5 個 Redis 節點的分散式鎖 | 需要較高的鎖正確性保證 |
| **DB Row Lock** | 利用 DB UNIQUE constraint + SELECT FOR UPDATE | 強一致，但效能受限 |

> Martin Kleppmann 在[其文章](https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html)中對 Redlock 的正確性提出質疑。若鎖的目的僅是效率（避免重複工作），單一 Redis 節點即足夠；若鎖的目的是正確性（絕不可重複），應以資料庫作為最終防線。

### 如何選擇？決策流程

```
你的業務邏輯能否與 idempotency key 放在同一個 DB transaction 中？
│
├─ 可以 → 你是否已有 DB 基礎設施且效能足夠？
│            │
│            ├─ 是 → 方案 B：RDBMS（最簡單，強一致）
│            │
│            └─ 否（需要更低延遲）→ 方案 C：混合
│
└─ 不行（涉及外部呼叫）
      │
      ├─ 重複執行的後果是否嚴重（如重複扣款）？
      │     │
      │     ├─ 是 → 方案 C：混合（Redis 鎖 + DB 持久化）
      │     │        並搭配 Atomic Phases 模型
      │     │
      │     └─ 否（可接受極端 edge case）→ 方案 A：Redis
      │
      └─ 你能否接受額外的運維成本？
            │
            ├─ 是 → 方案 C：混合
            └─ 否 → 方案 A：Redis（接受風險）或方案 B：RDBMS（接受延遲）
```

---

## 8. Request Fingerprint 驗證

### 為什麼需要

防止客戶端誤用同一個 Idempotency Key 發送**不同內容的請求**。

### 做法

1. 首次請求到達時，計算 request body 的 hash（fingerprint）並與 key 一起儲存
2. 後續重試到達時，計算新請求的 fingerprint 並與儲存值比對
3. 若不相符，拒絕請求並回傳錯誤

### Fingerprint 不匹配時的 HTTP Status Code 選擇

| Status Code | 語意 | 採用者 | 考量 |
|-------------|------|--------|------|
| `422 Unprocessable Entity` | 請求格式正確但語意錯誤 | Stripe | 強調「key 與 payload 的組合」不合法 |
| `409 Conflict` | 與伺服器當前狀態衝突 | 部分實作 | 強調「該 key 已被佔用」 |
| `400 Bad Request` | 客戶端請求有誤 | 部分實作 | 最通用，但語意較模糊 |

> **建議**：若你的 API 已有慣用的錯誤回傳風格，保持一致即可。若無特別偏好，`422` 的語意最精確——請求本身格式正確，但 idempotency key 與 request body 的「語意組合」無法被處理。

### Fingerprint 計算建議

- 對 request body 做 SHA-256 hash
- 排除不穩定欄位（如 timestamp、nonce）
- 排除無法序列化的參數（如 CancellationToken）
- 考慮是否納入 HTTP method 和 URL path

---

## 9. 錯誤處理策略

### 哪些錯誤該快取？

不是所有錯誤回應都應該被快取。關鍵在於區分**確定性錯誤**與**暫時性錯誤**：

| 錯誤類型 | 範例 | 是否快取 | 理由 |
|---------|------|---------|------|
| 驗證錯誤 (4xx) | 參數格式錯誤、權限不足 | 視設計而定 | 重試也不會成功，但 Stripe 的做法是不快取（因為業務邏輯尚未開始執行） |
| 業務邏輯錯誤（有副作用） | 扣款成功但後續步驟失敗 | **是** | 已產生副作用，必須記錄以避免重複執行 |
| 業務邏輯錯誤（無副作用） | 餘額不足（尚未扣款） | **視情況** | 見下方討論 |
| 暫時性錯誤 (5xx) | 超時、服務不可用 | **否** | 重試可能成功，快取會阻擋正常恢復 |

### 業務邏輯錯誤的細緻區分

「業務邏輯錯誤」不應被一概而論。關鍵判斷標準是：**該錯誤發生時，是否已產生副作用？**

| 情境 | 有副作用？ | 快取？ | 說明 |
|------|----------|--------|------|
| 檢查餘額不足，尚未扣款 | 否 | 不快取 | 使用者儲值後應能用同一 key 重試成功 |
| 扣款成功，但建立訂單失敗 | 是 | 快取 | 若不快取，重試會再扣一次款 |
| 庫存不足，尚未鎖定庫存 | 否 | 不快取 | 庫存補充後應能重試 |
| 已鎖定庫存，但出貨 API 失敗 | 是 | 快取 | 若不快取，重試會再鎖一次庫存 |

> **原則：有副作用就快取，無副作用可選擇不快取（刪除 key 讓客戶端重試）。** 但請注意，「無副作用」的判斷必須非常謹慎——若有任何疑慮，寧可快取。

### Stripe 的策略

Stripe 的做法較為特殊：**只在業務邏輯開始執行後才快取結果**。請求驗證階段的失敗不會被快取，讓客戶端可以修正參數後重試。

### 建議策略

```
收到請求
  ├─ 參數驗證失敗 → 直接回傳 4xx，不記錄 idempotency key
  ├─ 取得鎖失敗（併發）→ 回傳 409 Conflict
  └─ 開始執行業務邏輯
       ├─ 成功 → 快取回應，狀態設為 COMPLETED
       ├─ 確定性失敗 → 快取回應，狀態設為 FAILED
       └─ 暫時性失敗 → 刪除 key（或設為可重試），允許客戶端重試
```

---

## 10. TTL 與過期策略

### 建議 TTL

| 場景 | 建議 TTL | 說明 |
|------|---------|------|
| 一般 API 操作 | 24 小時 | 涵蓋大多數重試場景 |
| 金融 / 支付操作 | 24 - 72 小時 | 金融場景需更長的重試窗口 |
| 高吞吐量操作 | 1 - 6 小時 | 減少儲存壓力 |

### 鎖的 TTL（IN_PROGRESS 狀態）

鎖的 TTL 必須大於業務邏輯的**最大預期執行時間**，否則：

1. 請求 A 取得鎖，開始執行
2. 鎖因 TTL 過期而被釋放
3. 請求 B 取得鎖，也開始執行
4. 兩個請求都執行了 → **冪等性被破壞**

> 建議：鎖的 TTL 設為業務邏輯最大執行時間的 **2-3 倍**。

### 過期清理

- **Redis**：原生 TTL，自動過期，無需額外處理
- **RDBMS**：需建立排程任務（如 cron job）定期清理過期的 key

---

## 11. 基礎設施故障的降級策略

在生產環境中，Redis 或資料庫都可能發生故障。當冪等保護所依賴的基礎設施不可用時，系統該如何反應？

### 策略比較

| 策略 | 做法 | 適用場景 | 風險 |
|------|------|---------|------|
| **Fail-Closed（拒絕服務）** | 冪等儲存不可用時，拒絕所有寫入請求，回傳 `503 Service Unavailable` | 金融、支付等不可重複的高風險場景 | 可用性降低，影響所有使用者 |
| **Fail-Open（放行請求）** | 冪等儲存不可用時，跳過冪等檢查，直接執行業務邏輯 | 重複執行後果可控、有其他補償機制 | 短暫時間內可能出現重複執行 |
| **降級到本地快取** | 冪等儲存不可用時，fallback 到 Pod 本地的 in-memory cache | 過渡性方案，僅擋同一 Pod 的重複 | 跨 Pod 的重複請求無法防護 |

### 決策建議

```
冪等儲存不可用
│
├─ 重複執行的後果是否不可逆且嚴重（如重複扣款）？
│     │
│     ├─ 是 → Fail-Closed：寧可暫停服務，不可重複執行
│     │
│     └─ 否 → 是否有事後補償機制（如對帳、冪等的下游 API）？
│               │
│               ├─ 有 → Fail-Open：接受短暫風險，維持可用性
│               │
│               └─ 無 → Fail-Closed 或降級到本地快取
│
└─ 無論選擇哪種策略，都必須：
      - 發出告警通知 on-call 人員
      - 記錄降級期間的所有請求（含 idempotency key），以利事後稽核
      - 設定自動恢復機制（health check 偵測到恢復後自動切回正常模式）
```

---

## 12. 可觀測性

Idempotency Key 機制在生產環境中需要足夠的可觀測性，才能及時發現問題並進行調優。

### 建議追蹤的指標

| 指標 | 說明 | 告警時機 |
|------|------|---------|
| **Replay Ratio** | 快取命中（重播回應）佔總請求的比例 | 異常升高可能代表客戶端有 bug（無限重試） |
| **Key Collision Rate** | 不同 request body 使用相同 key 被拒絕的比例 | 升高代表客戶端 key 產生邏輯有問題 |
| **Lock Contention Rate** | 因併發衝突回傳 409 的比例 | 持續偏高可能需要調整鎖策略或業務流程 |
| **Lock Expiry Rate** | 鎖因 TTL 過期而被釋放（非正常釋放）的比例 | 任何非零值都需要調查——可能代表業務邏輯超時 |
| **Storage Latency** | 讀寫 idempotency key 儲存的延遲（p50/p95/p99） | 延遲升高可能影響整體 API 效能 |
| **Degradation Events** | 進入降級模式的次數與持續時間 | 任何降級都應告警 |

### 建議的日誌欄位

每個涉及 idempotency key 的請求日誌應包含：

- `idempotency_key`：key 值
- `idempotency_status`：`new` / `replay` / `conflict` / `fingerprint_mismatch`
- `lock_acquired`：是否成功取得鎖
- `lock_duration_ms`：持鎖時間（僅在正常釋放時記錄）

---

## 13. 常見反模式

### 13.1 只靠應用程式記憶體

```
// 反模式：In-memory dictionary
private static Dictionary<string, Response> _keys = new();
```

在多 Pod 環境下完全無效。每個 Pod 有自己的記憶體空間，無法跨 Pod 防重。

### 13.2 先檢查再執行（Check-then-Act）

```
// 反模式：非原子的 check-then-act
if (!await store.ExistsAsync(key))        // ← 兩個 Pod 都可能通過
{
    await store.SetAsync(key, "processing");
    await ExecuteBusinessLogic();          // ← 兩個 Pod 都執行了
}
```

必須使用原子操作（如 `SET NX`、`INSERT ... ON CONFLICT`）。

### 13.3 快取所有錯誤

快取暫時性錯誤（如 503 Service Unavailable）會阻擋客戶端的正常重試。

### 13.4 混淆 Correlation ID 與 Idempotency Key

| | Correlation ID | Idempotency Key |
|--|---------------|-----------------|
| 用途 | 追蹤 / 日誌關聯 | 防止重複執行 |
| 唯一性範圍 | 每次請求唯一 | 每個業務操作唯一（重試共用） |
| 伺服端行為 | 僅記錄，不影響邏輯 | 影響是否執行業務邏輯 |

### 13.5 Key 的 TTL 過短或不設 TTL

- 過短：客戶端在 TTL 內未完成重試，冪等性失效
- 不設：儲存無限成長，最終耗盡資源

### 13.6 未驗證 Request Fingerprint

允許相同 key 搭配不同 request body，可能導致邏輯錯誤或被惡意利用。

---

## 14. 業界參考實作

### Stripe

- 客戶端透過 `Idempotency-Key` header 傳遞 UUID v4
- 僅用於 POST 請求
- 快取首次回應（含 status code 和 body），24 小時後過期
- 比對請求參數，不匹配時報錯
- 業務邏輯執行前的驗證錯誤不快取

### Shopify

- 在回應中回傳 `X-Request-Id` 供追蹤
- 對 `POST` 請求支援 idempotency key
- 併發請求回傳 `409 Conflict`

### Adyen

- 使用 idempotency key 保護支付操作
- 快取時間 31 天
- 重複請求回傳與首次完全相同的回應

---

## 15. 架構決策清單

在設計你的 Idempotency Key 機制時，逐一確認以下決策：

- [ ] **Key 的傳遞方式**：HTTP Header（推薦 `Idempotency-Key`）或 Request Body？
- [ ] **Key 的格式**：UUID v4？其他隨機字串？
- [ ] **適用範圍**：哪些端點需要冪等保護？僅 POST？還是包含 PATCH？
- [ ] **儲存方案**：Redis / RDBMS / 混合？（參考[方案比較](#7-儲存方案比較)）
- [ ] **併發策略**：拒絕（409）或等待？
- [ ] **鎖機制**：Redis SET NX / Redlock / DB Row Lock？
- [ ] **鎖的 TTL**：業務邏輯最大執行時間的幾倍？
- [ ] **Key 的 TTL**：24 / 48 / 72 小時？
- [ ] **過期清理**：Redis 原生 TTL 或 DB 排程清理？
- [ ] **Fingerprint 驗證**：是否驗證？Hash 演算法？排除哪些欄位？
- [ ] **錯誤快取策略**：哪些錯誤快取、哪些允許重試？
- [ ] **回應標記**：是否加入 `X-Idempotent-Replay` header？
- [ ] **降級策略**：基礎設施故障時 Fail-Closed 或 Fail-Open？（參考[降級策略](#11-基礎設施故障的降級策略)）
- [ ] **可觀測性**：追蹤哪些指標？日誌包含哪些欄位？（參考[可觀測性](#12-可觀測性)）

---

## Sources

### 標準規範
- [IETF Draft: The Idempotency-Key HTTP Header Field (draft-07)](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)
- [Working with the new Idempotency Keys RFC - HTTP Toolkit](https://httptoolkit.com/blog/idempotency-keys/)

### 業界實作
- [Designing robust and predictable APIs with idempotency - Stripe Blog](https://stripe.com/blog/idempotency)
- [Idempotent requests - Stripe API Reference](https://docs.stripe.com/api/idempotent_requests)
- [Implementing Stripe-like Idempotency Keys in Postgres - brandur.org](https://brandur.org/idempotency-keys)
- [Implementing idempotency - Shopify](https://shopify.dev/docs/api/usage/implementing-idempotency)
- [API idempotency - Adyen Docs](https://docs.adyen.com/development-resources/api-idempotency)

### 系統設計
- [Idempotency | System Design - AlgoMaster.io](https://algomaster.io/learn/system-design/idempotency)
- [Advanced Idempotency in System Design - The Architect's Notebook](https://thearchitectsnotebook.substack.com/p/advanced-idempotency-in-system-design)
- [Grokking Idempotency in System Design - Design Gurus](https://www.designgurus.io/blog/grokking-idempotency)
- [Making retries safe with idempotent APIs - AWS Builders' Library](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/)

### 儲存與分散式鎖
- [What is idempotency in Redis? - Redis Blog](https://redis.io/blog/what-is-idempotency-in-redis/)
- [How to do distributed locking - Martin Kleppmann](https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html)
- [Distributed Locking: A Practical Guide - Oskar Dudycz](https://www.architecture-weekly.com/p/distributed-locking-a-practical-guide)
- [Redis + Distributed Lock for Idempotent APIs (.NET Practical Guide)](https://medium.com/the-syntax-hub/redis-distributed-lock-for-idempotent-apis-net-practical-guide-edef67f56210)

### 實作指南
- [Implementing Idempotency Keys in REST APIs - Zuplo](https://zuplo.com/learning-center/implementing-idempotency-keys-in-rest-apis-a-complete-guide)
- [Build an Idempotent API in Node.js with Redis - AppSignal](https://blog.appsignal.com/2024/02/14/build-an-idempotent-api-in-nodejs.html)
- [Idempotency in Distributed Spring Boot Apps Using MySQL - DZone](https://dzone.com/articles/implementing-idempotency-spring-boot-mysql)
- [Handling Race Conditions in Idempotent Operations - Medium](https://medium.com/@ankurnitp/handling-race-conditions-in-idempotent-operations-a-practical-guide-for-payment-systems-eb045b9ca7c4)

### 反模式與注意事項
- [Idempotency Keys: The One Thing Breaking Your API - Medium](https://medium.com/@mdfadil/idempotency-keys-the-one-thing-breaking-your-api-506d8d78fb34)
- [How to Design Idempotent APIs Safely - Medium](https://medium.com/@mathildaduku/how-to-design-idempotent-apis-safely-what-to-cache-and-what-to-ignore-feb93a16fc00)
- [Idempotency in API Design: Prevent Duplicates - Technori](https://technori.com/2026/02/24486-idempotency-in-api-design-prevent-duplicates/editorial-team/)
