# 分散式 Web API 冪等性（Idempotency Key）嚴格拒絕模式 - 最佳實踐指南

這份指南針對**多 Pod/Container 部署環境**，並根據特定的業務需求（嚴格拒絕重複請求、不檢驗 Payload、失敗可重試）量身打造，並加入了企業級架構的安全與容錯防禦機制。

---

## 1. 核心設計原則

在此設計模式中，Idempotency Key 的主要目的是**「防止操作被重複執行」**。
*   **儲存媒介**：Redis (利用其分散式與高效能特性)。
*   **重複請求回應**：一律回傳 HTTP `409 Conflict`，明確拒絕。
*   **Payload 檢查**：不檢查，只要 Key 存在即視為重複。
*   **失敗處理**：業務邏輯失敗（如 400 錯誤）時，安全地刪除 Key 允許重試。
*   **生命週期 (TTL)**：1 天（24 小時）。

---

## 2. 狀態機與錯誤碼設計 (Error Details)

雖然「處理中」與「已完成」的重複請求都回傳 HTTP `409 Conflict`，但**強烈建議在 Response Body 中加以區分**（參考 RFC 7807 Problem Details），以利客戶端決定後續動作。

| Redis 狀態 | 說明 | API 回應 (HTTP 409) 建議 Payload |
| :--- | :--- | :--- |
| **不存在 (None)** | 初次請求，允許執行。 | 正常執行 (200/201) |
| **處理中 (Processing)** | 其他 Pod 正在處理此請求。 | `{"code": "PROCESSING_CONFLICT", "message": "Request is currently being processed. Please try again later."}` |
| **已完成 (Completed)** | 該操作已成功執行完畢。| `{"code": "ALREADY_COMPLETED", "message": "This operation has already been completed and cannot be repeated."}` |

---

## 3. 分散式原子操作 (Redis Lua Scripts)

在多 Pod 併發的環境下，對 Redis 的操作必須保證絕對的原子性。我們需要兩段 Lua Script：一段用於**加鎖**，一段用於**安全解鎖**。

### A. 檢查與加鎖 (Lock & Proceed)
為防止不同 Pod 互相干擾，加鎖時必須寫入一個**專屬於當前 Pod/請求的唯一識別碼 (Worker ID)**。

```lua
-- KEYS[1]: 冪等性 Key (例如 idempotency:key:{request_id})
-- ARGV[1]: 狀態標記前綴 (例如 "PROCESSING:") 加上 Worker ID (例如 UUID)
-- ARGV[2]: 鎖定過期時間 (例如 60 秒，防止 Pod 崩潰導致永遠鎖死)

local status = redis.call("GET", KEYS[1])

if not status then
    -- 不存在，寫入 PROCESSING:{worker_id} 並設定短暫 TTL
    redis.call("SET", KEYS[1], ARGV[1], "EX", ARGV[2])
    return "PROCEED"
else
    -- 如果已經存在，回傳當前狀態讓後端判斷是 PROCESSING 還是 COMPLETED
    return status
end
```

### B. 業務失敗時的安全解鎖 (Compare-and-Delete)
**致命錯誤防範**：若業務失敗要刪除 Key，**絕對不能直接使用 `DEL`**。必須檢查該 Key 的 Value 是否還是自己當初寫入的 Worker ID，防止誤刪別人的鎖。

```lua
-- KEYS[1]: 冪等性 Key
-- ARGV[1]: 剛才加鎖時使用的 Worker ID (例如 "PROCESSING:1234-abcd")

if redis.call("GET", KEYS[1]) == ARGV[1] then
    -- 確定這把鎖還是我的，安全刪除
    return redis.call("DEL", KEYS[1])
else
    -- 鎖已經過期並被其他人拿走，或者已經變成 COMPLETED，什麼都不做
    return 0
end
```

---

## 4. 後端程式實作流程 (Pseudo Code)

```text
1. 攔截 Request，取得 `X-Idempotency-Key`。
2. 生成當前請求的唯一 `WorkerID` (UUID)。
3. 執行【加鎖 Lua 腳本】：
   - 若回傳結果為 `PROCEED` -> 繼續執行步驟 4。
   - 若回傳結果包含 `PROCESSING` -> 中斷，回傳 `409 (PROCESSING_CONFLICT)`。
   - 若回傳結果包含 `COMPLETED` -> 中斷，回傳 `409 (ALREADY_COMPLETED)`。

4. 執行業務邏輯 (資料庫寫入、呼叫外部 API 等)...

5. 根據業務執行結果更新 Redis：
   
   - 情況 A：業務成功 (200/201)
     -> 將 Redis 狀態覆寫為 "COMPLETED"，並將 TTL 延長至 1 天。
     -> `SET idempotency:key:{id} "COMPLETED" EX 86400`
     -> 回傳成功結果給前端。

   - 情況 B：業務邏輯失敗 (例如: 400 餘額不足) 或 系統崩潰 (500)
     -> 執行【安全解鎖 Lua 腳本】，傳入當初的 `WorkerID` 進行比對刪除。
     -> 允許前端修改資料後重試。
     -> 回傳原本的錯誤結果給前端。
```

---

## 5. 架構韌性與防禦機制 (Resilience & Defense in Depth)

1. **Redis 降級策略 (Graceful Degradation)**：
   如果 Redis 叢集無預警斷線，API 該如何反應？
   *   **Fail-Closed (嚴格阻擋)**：對於牽涉金流、扣款等高風險 API，若連不上 Redis 無法驗證冪等性，應直接回傳 `503 Service Unavailable`，拒絕處理請求。
   *   **Fail-Open (寬容放行)**：對於低風險操作（如更新使用者暱稱），若 Redis 故障，可選擇繞過冪等檢查直接執行，以維持系統可用性。

2. **資料庫唯一約束 (Defense in Depth)**：
   Redis 僅是記憶體快取，資料有遺失風險。作為系統的最後一道防線，核心業務資料庫必須建立 **Unique Constraint (唯一索引)**。例如：將 `Idempotency-Key` 或 `Order_ID` 存入 DB 並設為 Unique，即使 Redis 鎖失效，DB 寫入時依然會拋出 `DuplicateKeyException`，確保資料絕對安全。

3. **Key 的範圍與安全性**：
   *   驗證前端傳入的 Key 必須為有效的 UUID v4。
   *   限制 Key 的長度 (如不超過 36 字元)，防止惡意字串耗盡 Redis 記憶體。
   *   若系統有多個不同的 API 端點共用同一個 Redis，Key 命名應加上業務前綴，例如：`idp:orders:create:{uuid}`。
