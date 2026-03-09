# 升級版 Idempotency Key：用 Redis 實現分散式冪等保護

[上一篇](https://dotblogs.com.tw/yc421206/2022/10/09/how_to_impl_simple_idempotent_key_in_asp_net_6) 用 `IDistributedCache` + MemoryCache 做了一個簡單版本的冪等，適合單節點演練。但在多 Pod / Container 部署的環境下，MemoryCache 各自獨立，不同 Pod 看不到彼此的快取，冪等保護會直接失效。

這篇換用 **Redis** 來實現分散式冪等，目標是能跑在 Kubernetes / Docker Swarm 這類多副本環境。

---

## 與上一篇的差異

| 項目             | 上一篇（2022）                      | 這一篇                               |
|-----------------|-------------------------------------|--------------------------------------|
| 儲存層           | IDistributedCache (MemoryCache)     | Redis String（SET NX EX）            |
| 部署環境         | 單節點                               | 多 Pod / Container                   |
| 狀態模型         | 有快取 / 無快取（2 態）               | InProgress / Completed / Failed（3 態）|
| 原子操作         | 無（讀寫分兩步）                      | SET NX EX（初始鎖為原子操作）          |
| 請求指紋         | 無                                   | SHA-256 Fingerprint                  |
| 錯誤回應格式     | 自訂                                 | 標準 HTTP 狀態碼（400 / 409 / 422）   |
| 安全性           | 無 User 綁定                         | Key 可綁定 user_id（見進階章節）       |

---

## 需求與場景

電商付款是最典型的例子：

- 使用者按下「付款」，網路超時，前端自動重試
- Load Balancer 把重試打到不同的 Pod
- 兩個 Pod 同時收到「付款」請求 → **扣款兩次**

要解決這個問題，需要：

1. **跨 Pod 共享狀態**：Redis 作為唯一的真相來源
2. **原子性**：「檢查狀態 → 寫入 processing」必須是不可分割的操作
3. **防止重複內容攻擊**：同一個 Key 不能用在不同的請求 Body

---

## 測試場景

| # | 情境                                  | 預期結果                      |
|---|---------------------------------------|-------------------------------|
| 1 | 首次請求，無 Idempotency-Key Header    | 400 Bad Request               |
| 2 | 首次請求，有合法 Key                   | 200，執行業務邏輯，結果存 Redis |
| 3 | 相同 Key 第二次請求（已 completed）     | 200，直接回傳快取結果           |
| 4 | 相同 Key + 不同 Request Body           | 422 Unprocessable Content     |
| 5 | 兩個 Pod 同時送相同 Key                | 一個 200，另一個 409 Conflict  |
| 6 | Pod A 處理中崩潰，Pod B 重試           | processing TTL 到期後，重新執行 |
| 7 | Redis 掛掉（支付類 endpoint）          | 503 Service Unavailable       |

---

## 核心設計

### Request 處理流程

```
收到 API 請求
     │
     ├── 無 Idempotency-Key Header → 400 Bad Request
     │
     ▼
Redis SET NX EX（原子取鎖）
     │
     ├── 成功（Key 不存在）  → 首次請求，執行業務邏輯 → 存結果 → 200
     │
     └── 失敗（Key 已存在）→ GET 取出現有狀態
             │
             ├── InProgress         → 409 Conflict
             ├── Completed / Failed → 驗證 Fingerprint
             │       ├── 相符 → 回傳快取結果 → 200
             │       └── 不符 → 422 Unprocessable Content
```

### 3 態狀態機

```
  不存在 ──▶ InProgress ──▶ Completed
                │                │
                ▼                │ TTL 到期
             Failed              ▼
           (可重試)          自動刪除
```

- **InProgress**：請求進行中，TTL 建議 30~60 秒（防止 Pod 崩潰造成死鎖）
- **Completed**：已完成，TTL 建議 24 小時（Stripe 做法）
- **Failed**：業務失敗但可重播快取錯誤，TTL 同 Completed

### Redis 資料結構

這次實作使用 Redis String（`SET NX EX`），整個 Record 序列化成 JSON 存入：

```
Key:   idempotency:{idempotency_key}
Value: { "key": "...", "status": "InProgress", "requestFingerprint": "sha256:...",
         "responseStatusCode": null, "responseBody": null, "createdAt": "...", "expiresAt": "..." }
```

> **進階做法**：若需要綁定使用者、防止跨 user 存取，可將 Key 改成
> `idempotency:{user_id}:{method}:{path}:{idempotency_key}`。

### Fingerprint 計算

對 Method + Path + Action Arguments 做 SHA-256，防止同一個 Key 被不同內容的請求重複使用：

```csharp
var input = new {
    Method = request.Method,
    Path = request.Path.Value,
    Args = context.ActionArguments
        .Where(kv => kv.Value is not CancellationToken)
        .OrderBy(kv => kv.Key)
        .ToDictionary(kv => kv.Key, kv => kv.Value)
};
var json = JsonSerializer.Serialize(input);
var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
return Convert.ToHexString(hash).ToLowerInvariant();
```

---

## 關鍵實作

### TryAcquireAsync：SET NX EX 取得執行權

```csharp
// SET NX EX：原子操作，只有 key 不存在時才寫入
var acquired = await db.StringSetAsync(redisKey, value, ttl, When.NotExists);

if (acquired)
    return null; // 成功取得鎖，null 代表「首次請求」

// key 已存在，取出現有記錄回傳
var existing = await db.StringGetAsync(redisKey);
return JsonSerializer.Deserialize<IdempotencyKeyRecord>((string)existing!, jsonOptions);
```

**`SET NX EX` 是原子操作**，能保證「只有第一個 Pod 能寫入」。但後續的 `GET` 是獨立指令，中間有極小的窗口（Key 在 SET NX 失敗後、GET 之前過期）。程式碼對這個邊界情況有處理：重試一次 SET NX，若仍失敗再 GET。

### 兩種做法比較

| 做法             | 優點                         | 限制                                      |
|-----------------|------------------------------|-------------------------------------------|
| **SET NX EX**   | 簡單、相容所有 Redis 版本     | `GET` 是獨立指令，有極小競爭窗口（已處理）   |
| **Lua Script**  | 讀 + 寫真正一體原子，無窗口   | 需要 Redis Hash 結構，程式碼較複雜          |

對於大多數場景，`SET NX EX` 的競爭窗口發生機率極低（Key 需要在微秒內過期），已足夠。
若是支付等對正確性要求極高的場景，可考慮升級為 Lua Script + Redis Hash 做法。

### Lua Script 版本（進階）

若需要消除任何競爭窗口，可改用 Lua Script 將「讀狀態 → 判斷 → 寫狀態」合為單一原子操作：

```lua
-- KEYS[1] = Redis Key
-- ARGV[1] = request_hash, ARGV[2] = ttl_seconds, ARGV[3] = json_value

local existing = redis.call('GET', KEYS[1])

if existing == false then
    redis.call('SET', KEYS[1], ARGV[3])
    redis.call('EXPIRE', KEYS[1], tonumber(ARGV[2]))
    return cjson.encode({ result = 'ACQUIRED' })
end

local record = cjson.decode(existing)

if record.requestFingerprint ~= ARGV[1] then
    return cjson.encode({ result = 'FINGERPRINT_MISMATCH' })
end
if record.status == 'InProgress' then
    return cjson.encode({ result = 'PROCESSING' })
end

return cjson.encode({ result = record.status, data = existing })
```

---

## 錯誤回應

```http
HTTP/1.1 400 Bad Request
{ "error": "Idempotency-Key header is required" }
```

```http
HTTP/1.1 409 Conflict
{ "error": "A request with this idempotency key is already being processed. Retry after the original request completes." }
```

```http
HTTP/1.1 422 Unprocessable Content
{ "error": "Idempotency key has already been used with a different request payload." }
```

---

## 客戶端重試策略

| 收到的回應       | 客戶端行為                             |
|----------------|---------------------------------------|
| 網路超時         | 使用**相同** Key 重試                  |
| 409 Conflict    | 使用**相同** Key，等待後重試            |
| 2xx 成功        | 不重試                                 |
| 400 / 401 / 403 | 修正後用**新 Key**                     |
| 422             | 使用**新 Key**                         |
| 500 / 502 / 503 | 使用**相同 Key** 重試                  |

建議搭配 **Exponential Backoff with Jitter** 避免重試風暴：

```
delay = min(base × 2^attempt + jitter, 30s)
```

---

## Redis 故障策略

| 策略          | 行為                            | 適用場景         |
|--------------|--------------------------------|-----------------|
| **Fail-Open**  | Redis 不可用時跳過冪等，直接處理 | 一般操作         |
| **Fail-Closed**| Redis 不可用時回傳 503          | 支付、轉帳等      |

涉及金錢或不可逆操作的 endpoint 建議用 **Fail-Closed**。

---

## 監控建議

```json
{
  "event": "idempotency_check",
  "idempotency_key": "8e03978e-...",
  "endpoint": "POST /api/orders",
  "result": "ACQUIRED",
  "pod_id": "pod-abc-123",
  "duration_ms": 2
}
```

幾個值得注意的 Metrics：

- `idempotency_conflict_total` 突然飆升 → 業務邏輯執行時間太長或客戶端重試太頻繁
- `idempotency_processing_timeout` 持續出現 → Pod 頻繁崩潰或 TTL 設太短
- `idempotency_redis_error_total > 0` → 立即告警，檢查 Redis 連線

---

## 開發環境

- .NET 9
- Redis 7
- Docker Compose

---

## 心得

上一篇用 MemoryCache 可以快速演練邏輯，但一碰到多副本部署就沒辦法用了。這次最關鍵的改動是 **Lua Script**，讓「讀狀態 → 判斷 → 寫狀態」這三步變成一個原子操作，這樣不管幾個 Pod 同時打進來，都只會有一個拿到 `ACQUIRED`。

Fingerprint 是另一個重要機制，防止客戶端不小心把同一個 Key 用在不同的請求 Body 上，這個問題在上一篇完全沒有處理。

---

## 參考資料

- [IETF Draft: The Idempotency-Key HTTP Header Field](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)
- [RFC 7807: Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807)
- [Stripe: Idempotent Requests](https://stripe.com/docs/api/idempotent_requests)
- [Brandur: Implementing Stripe-like Idempotency Keys in Postgres](https://brandur.org/idempotency-keys)
- [上一篇：在 ASP.NET 6 實作簡易版 Idempotent Key](https://dotblogs.com.tw/yc421206/2022/10/09/how_to_impl_simple_idempotent_key_in_asp_net_6)

若有謬誤，煩請告知，新手發帖請多包涵
