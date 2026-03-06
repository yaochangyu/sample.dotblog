# Idempotency Key 最佳實踐

> 參考來源：[IETF draft-ietf-httpapi-idempotency-key-header-07](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)、[Stripe API Docs](https://stripe.com/docs/api/idempotent_requests)、[Brandur Leach – Idempotency Keys](https://brandur.org/idempotency-keys)、[AWS Builders' Library – Making retries safe with idempotent APIs](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/)

---

## 1. 什麼是 Idempotency Key

Idempotency Key 是由 **client 產生**、隨請求傳送給 server 的唯一識別碼。  
Server 以此 key 辨識「同一次操作的重複請求」，確保副作用（side effect）只執行一次。

**核心目標**：在網路不穩定、連線逾時、client 不確定請求是否成功時，允許安全地重試，不造成重複扣款、重複建立資料等問題。

---

## 2. 適用場景

| HTTP 方法 | 天生冪等？ | 需要 Idempotency Key？ |
|-----------|-----------|----------------------|
| GET       | ✅ 是     | 不需要                |
| PUT       | ✅ 是     | 不需要                |
| DELETE    | ✅ 是     | 不需要                |
| **POST**  | ❌ 否     | **需要**              |
| **PATCH** | ❌ 否     | **需要**              |

> RFC 9110：OPTIONS、HEAD、GET、PUT、DELETE 天生具有冪等性；POST 與 PATCH 不具備。

---

## 3. Key 的產生規範（Client 端）

### 3.1 格式建議

- **建議使用 UUID v4**（隨機性足夠，碰撞機率趨近於零）
- 或其他具有足夠 entropy 的隨機字串
- IETF 草案要求 key 必須是 Structured Header 中的 String 型別

```
Idempotency-Key: "8e03978e-40d5-43e8-bc93-6894a57f9324"
```

### 3.2 每次操作必須產生新的 Key

- 每一次「**使用者意圖**」對應一個新的 key
- 重試同一次操作使用**相同的 key**
- 不同的操作絕對不能重用舊的 key

```
# ✅ 正確：第一次付款
POST /payments
Idempotency-Key: "uuid-for-payment-A"

# ✅ 正確：第一次付款逾時，重試
POST /payments
Idempotency-Key: "uuid-for-payment-A"   ← 相同 key

# ❌ 錯誤：第二次不同的付款，不能重用
POST /payments
Idempotency-Key: "uuid-for-payment-A"   ← 不同操作卻用同一個 key
```

### 3.3 Key 長度限制

- Stripe 限制：255 字元
- brandur.org 範例：100 字元
- 建議在 API 文件中明訂最大長度並在 server 端驗證

---

## 4. Server 端實作規範

### 4.1 HTTP Header 名稱

IETF 草案標準化的 header 名稱為：

```
Idempotency-Key: "<value>"
```

> Stripe 使用 `Idempotency-Key`，部分廠商使用 `X-Idempotency-Key`。建議遵循 IETF 草案使用 `Idempotency-Key`。

### 4.2 Cache Key 設計

Server 端的快取 key 應包含足以唯一識別請求的資訊：

```
{prefix}:{HTTP_METHOD}:{PATH}:{idempotency_key}
```

範例：
```
Idempotent:POST:/api/payments:8e03978e-40d5-43e8-bc93-6894a57f9324
```

> **是否需要包含 User ID？**  
> - 若 idempotency key 由 client 以 UUID 產生，全域唯一性已足夠，不強制包含 User ID。  
> - 若 key 採用短字串或業務語義字串（如 `order-123`），**必須包含 User ID** 避免跨帳號碰撞。  
> - 建議在 API 文件中明訂 key 的格式規範。

### 4.3 快取內容

至少需快取：
- HTTP Status Code
- Response Body（JSON）
- 原始 Response Headers（還原 `ETag`、`X-Request-Id` 等自訂 header）

### 4.4 什麼情況下應快取

| 情境 | 行為 |
|------|------|
| **2xx**（200、201 等）成功回應 | ✅ 快取，後續重試直接回傳快取結果 |
| **4xx** 客戶端錯誤（驗證失敗等）| ⚠️ 視情況；Stripe 不快取，IETF 草案建議快取 |
| **5xx** Server 錯誤 | ❌ 不快取，讓 client 可以重試 |
| 並發請求（原始請求尚未完成）| 回傳 **409 Conflict** |

> **Stripe 的做法**：快取所有回應，包含 5xx，  
> **brandur 的建議**：5xx 不快取，允許重試。  
> 選擇哪種策略取決於業務需求，**須在 API 文件中明訂並保持一致**。

### 4.5 Request Fingerprint（請求指紋）

若同一個 idempotency key 搭配**不同的請求 payload**，server 應拒絕該請求：

- 計算 payload 的 checksum 或比對關鍵欄位
- 回傳 **HTTP 422 Unprocessable Content**

```json
HTTP/1.1 422 Unprocessable Content
{
  "type": "https://developer.example.com/idempotency",
  "title": "Idempotency-Key is already used",
  "detail": "Idempotency Key MUST not be reused across different payloads."
}
```

### 4.6 Key 過期策略

- 建議保留時間：**24 小時**（Stripe 的做法）
- 過期後視為新請求重新處理
- 在 API 文件中公開過期時間

---

## 5. 錯誤回應規範（IETF 草案）

| 情境 | HTTP Status |
|------|-------------|
| 缺少 `Idempotency-Key` header | **400 Bad Request** |
| Key 重複使用但 payload 不同 | **422 Unprocessable Content** |
| 原始請求仍在處理中（並發重試）| **409 Conflict** |

錯誤格式建議使用 [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807)：

```json
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://api.example.com/errors/idempotency",
  "title": "Idempotency-Key is missing",
  "detail": "This operation requires an Idempotency-Key header."
}
```

---

## 6. 並發請求處理（Locking）

當兩個請求同時帶著相同的 idempotency key 到達時，必須防止 race condition：

### 策略一：分散式鎖（Redis `SET NX`）

```
1. 嘗試對 key 加鎖（SET NX，TTL = 請求最大執行時間）
2. 加鎖成功 → 執行業務邏輯
3. 加鎖失敗（另一個請求正在處理）→ 回傳 409 Conflict
4. 執行完成後，將結果寫入快取，釋放鎖
```

### 策略二：資料庫 Unique Constraint

```sql
CREATE TABLE idempotency_keys (
    idempotency_key TEXT NOT NULL,
    user_id         BIGINT NOT NULL,
    locked_at       TIMESTAMPTZ,
    response_code   INT,
    response_body   JSONB,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uk_user_key UNIQUE (user_id, idempotency_key)
);
```

> 利用 DB 的 Unique Constraint 天然防止並發 insert，並用 `locked_at` 欄位標記正在處理中的請求。

---

## 7. 原子性與 Foreign State Mutation

> 這是 brandur Leach 在 Stripe 工作時整理的核心概念。

### 問題
當一個請求需要：
1. 寫入本地資料庫
2. 呼叫外部服務（如 Stripe、發送 Email）

若步驟 2 之後系統崩潰，資料庫狀態與外部系統狀態可能不一致。

### 解法：Atomic Phase + Recovery Point

將請求拆分成多個**原子階段（Atomic Phase）**，每個階段完成後記錄**還原點（Recovery Point）**：

```
[tx1] 寫入 idempotency_key（recovery_point = "started"）
[tx2] 建立業務資料（recovery_point = "data_created"）
[外部] 呼叫 Stripe 建立扣款
[tx3] 記錄扣款結果（recovery_point = "charge_created"）
[背景] 發送通知 Email（放入 job queue）
[tx4] 更新 idempotency_key（recovery_point = "finished"）
```

重試時根據 `recovery_point` 從中斷處繼續，而不是從頭重來。

### 規則
1. **每個外部狀態變更（foreign state mutation）各自成一個原子階段**
2. 原子階段使用 ACID 資料庫事務保護
3. **能非同步執行的外部呼叫（如 Email）應放入 background job queue**

---

## 8. 現有實作的對照分析

本專案（`IdempotentAttributeFilter`）採用 **HybridCache（記憶體 + Redis）** 實作，適合以下情境：

| 條件 | 本專案做法 |
|------|-----------|
| 快取範圍 | 2xx 回應 |
| 過期時間 | DI 注入（預設 60 秒，可 per-endpoint 設定）|
| 並發保護 | ❌ 未實作（HybridCache 不保證原子性）|
| Request Fingerprint | ❌ 未實作 |
| 持久化 | Redis（重啟後快取仍在）|

> **適合場景**：短時間內防止重複提交（防手抖、防網路重試）  
> **不適合場景**：需要嚴格保證的金融交易、涉及外部服務呼叫的複雜流程

若需要更嚴格的 idempotency 保證，建議：
- 加入分散式鎖防止並發
- 改用資料庫持久化 idempotency key 記錄
- 實作 recovery point 機制

---

## 9. 快速檢查清單

### Client 端
- [ ] 每次操作前產生唯一的 UUID v4 作為 idempotency key
- [ ] 重試時使用相同的 key
- [ ] 不同操作不重用 key
- [ ] 處理 409 Conflict（等待後重試，不換新 key）

### Server 端
- [ ] 驗證 `Idempotency-Key` header 是否存在，缺少時回傳 400
- [ ] 明訂 key 的格式規範（長度、建議使用 UUID）
- [ ] 明訂 key 的過期時間並公開於 API 文件
- [ ] 明訂哪些狀態碼會被快取
- [ ] 實作 request fingerprint 驗證，payload 不同時回傳 422
- [ ] 處理並發請求，回傳 409
- [ ] Cache hit 時還原所有 response headers

---

## 10. 參考資料

| 資料來源 | 說明 |
|---------|------|
| [IETF draft-ietf-httpapi-idempotency-key-header-07](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/) | HTTP Idempotency-Key header 標準草案（2025） |
| [Stripe – Idempotent Requests](https://stripe.com/docs/api/idempotent_requests) | Stripe 的實作設計與規範 |
| [Brandur Leach – Idempotency Keys](https://brandur.org/idempotency-keys) | Atomic Phase 與 Recovery Point 深度解析 |
| [AWS Builders' Library](https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/) | AWS 的 ClientToken 設計與 semantic equivalence |
