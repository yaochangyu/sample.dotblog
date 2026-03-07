# Code Review 報告

> 審查日期：2026-03-07
> 審查依據：`idempotency-key-best-practices-claude.md`

## 架構概覽

- ASP.NET Core 10 MVC + PostgreSQL (EF Core) + Redis
- Idempotency 機制：`IdempotencyKeyAttribute`（Action Filter）
- 兩種 Store 實作：`EfIdempotencyKeyStore`、`RedisIdempotencyKeyStore`

---

## 問題清單

### 🔴 嚴重

---

#### [ ] 1. `EfIdempotencyKeyStore` 是死碼，從未被使用

**位置**：`Program.cs:19`

```csharp
// Program.cs 只註冊 Redis，EF Store 完全未使用
builder.Services.AddSingleton<IIdempotencyKeyStore, RedisIdempotencyKeyStore>();
```

`EfIdempotencyKeyStore` 有完整實作、有 Migration，卻沒有任何地方使用它。這造成程式碼混亂，也讓 Migration 產生了一張不必要的 `IdempotencyKeys` 資料表（若目前只用 Redis）。

**建議**：若已決定使用 Redis，刪除 `EfIdempotencyKeyStore`；若要保留作為可替換選項，應透過設定檔切換，並加上清楚的說明。

---

#### [ ] 2. `RedisIdempotencyKeyStore.UpdateRecordAsync` 非原子操作（TOCTOU）

**位置**：`RedisIdempotencyKeyStore.cs:78-99`

```csharp
private async Task UpdateRecordAsync(...)
{
    var ttl = await db.KeyTimeToLiveAsync(redisKey);   // 1. GET TTL
    var existing = await db.StringGetAsync(redisKey);  // 2. GET value
    // ... 修改 record ...
    await db.StringSetAsync(redisKey, value, ttl);     // 3. SET value
}
```

這是三個獨立的 Redis 操作，非原子。兩個 request 並發完成時，可能互相覆蓋更新結果。依照最佳實踐 §3.1，狀態更新應使用 Lua script 確保原子性（`GET` + `SET` 在同一個 script 中執行）。

---

#### [ ] 3. `DbConcurrency` 暫時性錯誤被快取為 FAILED

**位置**：`FailureCodeMapper.cs:11`、`IdempotencyKeyAttribute.cs:158-168`

```csharp
// FailureCodeMapper: DbConcurrency → 409 Conflict
[nameof(FailureCode.DbConcurrency)] = HttpStatusCode.Conflict,

// IdempotencyKeyAttribute: 4xx 全部快取為 FAILED
if (statusCode >= 400)
    await store.SetFailedAsync(...);
```

`DbConcurrency` 是暫時性錯誤，應刪除 key 讓客戶端重試。但它被 `FailureCodeMapper` 對應成 409，因此 Filter 會將其快取為 `FAILED`，阻止客戶端重試，**冪等性因此被破壞**。

依照最佳實踐 §9：暫時性失敗應刪除 key（或標記可重試），而非快取。

---

### 🟡 中等

---

#### [ ] 4. 所有 4xx 錯誤皆快取，違反 Stripe 策略

**位置**：`IdempotencyKeyAttribute.cs:158-168`

```csharp
if (statusCode >= 400)
    await store.SetFailedAsync(...);  // DuplicateEmail (409) 也會被快取
```

`DuplicateEmail` 的檢查發生在任何 DB 寫入之前，沒有產生任何副作用。根據最佳實踐 §9 的 Stripe 策略：

> 業務邏輯開始執行前的失敗，不應快取，讓客戶端可修正後重試。

目前 `DuplicateEmail` 被快取後，同一個 key 無法在使用者改了 Email 後重試成功。

**建議**：從 Controller/Handler 回傳額外的 metadata 告知 Filter 是否有副作用，或針對特定錯誤碼採用不同的快取策略。

---

#### [ ] 5. `MemberHandler.CreateAsync` 使用 Check-then-Act 反模式

**位置**：`MemberHandler.cs:17-23`

```csharp
var duplicate = await db.Members.AnyAsync(m => m.Email == request.Email, ct);
if (duplicate)
    return Result.Failure<Member, Failure>(...DuplicateEmail...);

// ← 這之間另一個請求可能插入相同 Email
return await repository.AddAsync(member, ct);
```

符合最佳實踐 §13.2 的反模式描述。雖然 DB 有 unique constraint 兜底，但底層拋出的 `DbUpdateException` 在 `AddAsync` 中被 catch 成 `DbError` → HTTP 500，而非正確的 `DuplicateEmail` → HTTP 409。

這會讓 Idempotency Filter 把 Email 衝突誤判為暫時性錯誤並刪除 key。

---

#### [ ] 6. EF Store 沒有過期 Key 的清理機制

**位置**：`EfIdempotencyKeyStore.cs`、`MemberDbContext.cs:27`

```csharp
entity.HasIndex(k => k.ExpiresAt); // 加速過期清理查詢 ← 只有 index，沒有清理邏輯
```

依照最佳實踐 §10：RDBMS 需要建立排程任務（cron job）定期清理過期 key。目前只有 index 沒有清理機制，長期下去 `IdempotencyKeys` 資料表會無限成長。

---

### 🔵 輕微

---

#### [ ] 7. Fingerprint 未包含 HTTP Method 和 URL Path

**位置**：`IdempotencyKeyAttribute.cs:173-183`

```csharp
var args = context.ActionArguments
    .Where(kv => kv.Value is not CancellationToken)
    // 未包含 Method / Path
```

最佳實踐 §8 建議：「考慮是否納入 HTTP method 和 URL path」。目前若同一個 key 不小心被用於兩個不同端點，Fingerprint 可能會誤判為相符（若兩個端點的 request body 結構相同）。

---

#### [ ] 8. `Idempotency-Key` header 值未驗證格式

**位置**：`IdempotencyKeyAttribute.cs:47-62`

只驗證非空白，沒有驗證格式（如 UUID v4）。IETF draft 建議使用具有足夠 entropy 的隨機字串，惡意客戶端可傳入空字串以外的任意值。

---

#### [ ] 9. `Program.cs` 同時引入 EF DbContext（含 IdempotencyKeys 表）和 Redis Store，但兩者語意衝突

若使用 Redis Store，Migration 產生的 `IdempotencyKeys` 資料表是多餘的；若日後切換為 EF Store，Redis 中的資料又無法遷移。兩種儲存機制的選擇應在設定層次明確分離。

---

## 整體評分

| 面向 | 評估 |
|------|------|
| 原子性（Atomicity） | ⚠️ Redis UpdateRecord 非原子 |
| 狀態機模型 | ✅ 正確實作三狀態 |
| 多 Pod 支援 | ✅ Redis 共享儲存 |
| Fingerprint 驗證 | ✅ SHA-256，已排除 CancellationToken |
| 錯誤快取策略 | ⚠️ 暫時性錯誤與業務錯誤未區分 |
| 回應標記 | ✅ X-Idempotent-Replay |
| TTL 管理 | ✅ Redis 原生 TTL；EF 缺清理機制 |
| 程式碼整潔 | ⚠️ EfIdempotencyKeyStore 是死碼 |

---

## 修正優先順序

| 優先級 | 問題 | 影響 |
|--------|------|------|
| P0 | #3 DbConcurrency 被快取為 FAILED | 冪等性被破壞，客戶端無法重試 |
| P0 | #2 Redis UpdateRecord 非原子 | 並發下狀態可能被覆蓋 |
| P1 | #1 EfIdempotencyKeyStore 是死碼 | 程式碼混亂，Migration 產生多餘資料表 |
| P1 | #5 Check-then-Act 反模式 | Email 衝突被誤判為 DB 錯誤 → 500 |
| P2 | #4 所有 4xx 皆快取 | 無副作用的業務錯誤無法重試 |
| P2 | #6 EF Store 無清理機制 | 資料表無限成長 |
| P3 | #7 Fingerprint 缺少 Method/Path | 跨端點的 key 誤用無法偵測 |
| P3 | #8 header 格式未驗證 | 接受任意字串作為 key |
| P3 | #9 兩種 Store 語意衝突 | 設定模糊，維護困難 |
