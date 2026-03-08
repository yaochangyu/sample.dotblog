# 冪等鍵改善計畫

> 依據：`code-review.md`  
> 計畫日期：2026-03-07  
> 目標版本：ASP.NET Core 10 / StackExchange.Redis

---

## 問題分析與立場

### 審查意見 #4 的評估

審查意見正確。但需補充更完整的根因分析：

**#3 與 #4 源自同一根因：`Failure` 缺乏「是否有副作用」的語意。**

目前 `IdempotencyKeyAttribute` 的快取決策只依賴 HTTP 狀態碼：

```
>= 500 → 刪除 key（視為暫時性失敗）
4xx    → 快取為 FAILED（視為確定性失敗）
2xx    → 快取為 COMPLETED
```

這個策略忽略了一個維度：**錯誤發生時，業務副作用是否已產生？**

| 錯誤 | 目前行為 | 正確行為 | 原因 |
|------|----------|----------|------|
| `DuplicateEmail` | 快取為 FAILED → 無法重試 | 刪除 key → 允許重試 | 檢查發生在 `AddAsync` 之前，DB 沒有任何寫入 |
| `DbConcurrency` | 快取為 FAILED → 無法重試 | 刪除 key → 允許重試 | `SaveChanges` 拋出例外，事務已回滾，沒有持久化 |
| `NotFound` | 快取為 FAILED → 重播 404 | 快取為 FAILED（正確）| 查詢類錯誤，重播語意清晰 |

**額外發現（#3 × #5 交叉問題）**

若修正 Check-then-Act (#5)，在 `MemberRepository.AddAsync` 中改為依賴 DB unique constraint 捕捉重複 Email，則 `DbUpdateException(23505)` 目前會落入 `catch (Exception ex)` → 回傳 `DbError` → HTTP 500 → **key 被刪除而非回傳 409**。這使得 #5 的修正若與 #3/#4 修正順序不對，會引入新的 bug。**必須同步修正。**

---

## 改善計畫

### Phase 1 — P0：修復冪等性核心（必須優先）

---

#### [x] Task 1.1：建立統一的 Retryable Failure 機制（修復 #3 + #4）

**目標**：讓每個 `Failure` 帶有「是否應刪除 key 允許重試」的語意，由業務層決定，Filter 執行。

**影響檔案**：
- `Failure.cs`
- `FailureCode.cs`（or `FailureCodeMapper.cs`）
- `MemberHandler.cs`（或下方 Repository）
- `MemberController.cs`
- `IdempotencyKeyAttribute.cs`

**設計**：使用 `HttpContext.Items` 作為業務層與 Filter 之間的通訊橋梁，避免污染 HTTP response header（不洩漏內部語意到 public API）。

**步驟 1**：在 `Failure` 加入 `IsRetryable` 屬性

```csharp
public class Failure
{
    // ... 現有屬性 ...

    /// <summary>
    /// 若為 true，表示此錯誤發生時尚無業務副作用，
    /// Idempotency Filter 應刪除 key 讓客戶端可修正後以相同 key 重試。
    /// </summary>
    [JsonIgnore]
    public bool IsRetryable { get; init; }
}
```

**步驟 2**：標記各錯誤碼的 retryable 語意

在 `MemberHandler` 建立 `Failure` 時設定：

| 錯誤碼 | `IsRetryable` | 理由 |
|--------|---------------|------|
| `DuplicateEmail` | `true` | 發生在任何 DB 寫入前，無副作用 |
| `DbConcurrency` | `true` | 事務已回滾，無持久化，可安全重試 |
| `NotFound` | `false` | 查詢結果確定，重播語意正確 |
| `ValidationError` | `true` | 客戶端格式錯誤，修正後可重試 |
| `DbError` | `false` | 已走 5xx 路徑，由 Filter 的 `>= 500` 邏輯刪除 |

**步驟 3**：`MemberController` 在回傳失敗時設定 `HttpContext.Items`

```csharp
// MemberController 中抽取共用方法
private IActionResult ToActionResult<T>(Result<T, Failure> result, Func<T, IActionResult> onSuccess)
{
    if (result.IsFailure)
    {
        if (result.Error.IsRetryable)
            HttpContext.Items["Idempotency:ShouldDeleteKey"] = true;

        return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
    }
    return onSuccess(result.Value);
}
```

**步驟 4**：`IdempotencyKeyAttribute.HandleActionResult` 讀取此旗標

```csharp
// 在 statusCode >= 400 的判斷之前
if (executedContext.HttpContext.Items.ContainsKey("Idempotency:ShouldDeleteKey"))
{
    logger.LogInformation(
        "Retryable failure (HTTP {StatusCode}) for key {Key}, deleting key to allow retry",
        statusCode, idempotencyKey);
    await store.DeleteAsync(idempotencyKey, ct);
    return;
}

if (statusCode >= 400)
    await store.SetFailedAsync(...);
```

---

#### [~] Task 1.2：Redis `UpdateRecordAsync` 改用 Lua script（修復 #2）

> **決策：暫緩。** 24hr TTL 下，TOCTOU 窗口（毫秒級）幾乎不可能觸發，生產風險可接受。  
> 屬防禦性程式設計，非緊急修復。若未來支援短 TTL（分鐘級）再處理。

**目標**：將三步驟的 GET TTL → GET value → SET value 改為原子操作，防止並發競爭。

**影響檔案**：`RedisIdempotencyKeyStore.cs`

**設計**：使用 Lua script，在單一 Redis 呼叫中完成「讀取現有值 → 更新欄位 → 以原 TTL 寫回」。

```lua
-- KEYS[1] = redis key
-- ARGV[1] = new status
-- ARGV[2] = response status code
-- ARGV[3] = response body
-- ARGV[4] = response content type
local existing = redis.call('GET', KEYS[1])
if not existing then return nil end
local ttl = redis.call('PTTL', KEYS[1])
local record = cjson.decode(existing)
record['status'] = ARGV[1]
record['responseStatusCode'] = tonumber(ARGV[2])
record['responseBody'] = ARGV[3]
record['responseContentType'] = ARGV[4]
local updated = cjson.encode(record)
if ttl > 0 then
    redis.call('SET', KEYS[1], updated, 'PX', ttl)
else
    redis.call('SET', KEYS[1], updated)
end
return updated
```

在 `RedisIdempotencyKeyStore` 中：
1. 將 Lua script 定義為 `private static readonly string UpdateScript = "..."` 
2. 使用 `db.ScriptEvaluateAsync(UpdateScript, keys, args)` 取代三步驟操作
3. 若 script 回傳 `nil`（key 不存在），則執行 fallback 重新 SET

---

### Phase 2 — P1：修復業務邏輯正確性

---

#### [ ] Task 2.1：移除 Check-then-Act 反模式（修復 #5）

**目標**：刪除 `MemberHandler` 的 pre-check，改為在 Repository 層依賴 DB unique constraint，並正確將 `23505` 例外轉換為 `DuplicateEmail`。

**注意**：**必須在 Task 1.1 完成後執行**，否則 `DuplicateEmail` 的 retryable 語意尚未正確設定，修正 #5 反而會引入 key 被誤刪的 bug（SaveChanges 拋出 `DbUpdateException` 走入 `DbError` 路徑）。

**影響檔案**：`MemberHandler.cs`、`MemberRepository.cs`

**步驟 1**：刪除 `MemberHandler.CreateAsync` 和 `UpdateAsync` 的 pre-check

```csharp
// 刪除：
var duplicate = await db.Members.AsNoTracking().AnyAsync(m => m.Email == request.Email, ct);
if (duplicate)
    return Result.Failure<Member, Failure>(new Failure { Code = nameof(FailureCode.DuplicateEmail), ... });
```

**步驟 2**：在 `EfMemberRepository.AddAsync` 捕捉 unique constraint 例外

```csharp
catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
{
    return Result.Failure<Member, Failure>(new Failure
    {
        Code = nameof(FailureCode.DuplicateEmail),
        Message = $"Email 已被使用",
        IsRetryable = true   // ← 搭配 Task 1.1
    });
}

private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    => ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
```

同理修正 `UpdateAsync`。

---

#### [ ] Task 2.2：EfIdempotencyKeyStore 設定化（修復 #1 + #9）

**目標**：透過 `appsettings.json` 決定使用哪種 Store，消除「死碼 + 語意衝突」問題。

**影響檔案**：`Program.cs`、`appsettings.json`、`appsettings.Development.json`

**設計**：

```json
// appsettings.json
{
  "IdempotencyStore": "Redis"  // "Redis" | "EF"
}
```

```csharp
// Program.cs
var storeType = builder.Configuration["IdempotencyStore"] ?? "Redis";
if (storeType == "EF")
    builder.Services.AddScoped<IIdempotencyKeyStore, EfIdempotencyKeyStore>();
else
    builder.Services.AddSingleton<IIdempotencyKeyStore, RedisIdempotencyKeyStore>();
```

並在 `README` 或 XML 文件中說明兩種 Store 的適用情境（開發/測試用 EF，生產用 Redis）。

---

### Phase 3 — P2/P3：強化與維護性

---

#### [ ] Task 3.1：EF Store 增加清理背景服務（修復 #6）

**目標**：定期清除 `IdempotencyKeys` 資料表中已過期的紀錄。

**影響檔案**：新增 `IdempotencyKeys/IdempotencyKeyCleanupService.cs`

```csharp
public class IdempotencyKeyCleanupService(IServiceScopeFactory scopeFactory, ILogger<...> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MemberDbContext>();
            var deleted = await db.IdempotencyKeys
                .Where(k => k.ExpiresAt < DateTimeOffset.UtcNow)
                .ExecuteDeleteAsync(stoppingToken);
            logger.LogInformation("Cleaned up {Count} expired idempotency keys", deleted);
        }
    }
}
```

在 `Program.cs` 中，當 `IdempotencyStore == "EF"` 時才註冊此 Service。

---

#### [ ] Task 3.2：Fingerprint 加入 HTTP Method + Path（修復 #7）

**目標**：防止同一個 idempotency key 在不同端點之間誤判為 fingerprint 相符。

**影響檔案**：`IdempotencyKeyAttribute.cs`

```csharp
private static string ComputeFingerprint(ActionExecutingContext context)
{
    var request = context.HttpContext.Request;
    var args = context.ActionArguments
        .Where(kv => kv.Value is not CancellationToken)
        .OrderBy(kv => kv.Key)
        .ToDictionary(kv => kv.Key, kv => kv.Value);

    var input = new
    {
        Method = request.Method,
        Path = request.Path.Value,
        Args = args
    };

    var json = JsonSerializer.Serialize(input);
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
    return Convert.ToHexString(hash).ToLowerInvariant();
}
```

---

#### [ ] Task 3.3：`Idempotency-Key` header 格式驗證（修復 #8）

**目標**：拒絕不符合格式的 key，減少惡意或誤用的 key 佔用儲存空間。

**影響檔案**：`IdempotencyKeyAttribute.cs`

```csharp
// 驗證：非空白 + 長度限制 + UUID v4 格式（可依需求調整為僅長度限制）
private static readonly Regex ValidKeyPattern =
    new(@"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

// 在讀取 header 後加入：
if (!ValidKeyPattern.IsMatch(idempotencyKey))
{
    context.Result = new BadRequestObjectResult(new
    {
        error = $"{IdempotencyKeyHeader} must be a valid UUID v4"
    });
    return;
}
```

---

## 執行順序與依賴關係

```
Task 1.1 (Retryable Failure 機制)
    ↓
Task 1.2 (Redis Lua)   Task 2.1 (移除 Check-then-Act)  ← 依賴 1.1 完成
    ↓                       ↓
Task 2.2 (Store 設定化)
    ↓
Task 3.1 (EF 清理) ← 依賴 2.2（只在 EF mode 下啟用）
Task 3.2 (Fingerprint)
Task 3.3 (Header 驗證)
```

| Task | Priority | 依賴 | 影響範圍 |
|------|----------|------|----------|
| 1.1 Retryable Failure 機制 | P0 | 無 | Failure.cs, Controller, Attribute |
| 1.2 Redis Lua script | P0 | 無 | RedisIdempotencyKeyStore.cs |
| 2.1 移除 Check-then-Act | P1 | 1.1 | MemberHandler, MemberRepository |
| 2.2 Store 設定化 | P1 | 無 | Program.cs, appsettings |
| 3.1 EF 清理 Service | P2 | 2.2 | 新增 CleanupService |
| 3.2 Fingerprint Method+Path | P3 | 無 | IdempotencyKeyAttribute.cs |
| 3.3 Header 格式驗證 | P3 | 無 | IdempotencyKeyAttribute.cs |

---

## 補充：對審查意見 #4 的完整立場

審查意見 #4 的核心觀察是正確的，但需要更精確地定義「有無副作用」：

- ✅ **無副作用 → 刪除 key**：`DuplicateEmail`（pre-check）、`DbConcurrency`（事務回滾）、`ValidationError`
- ✅ **有副作用 → 快取 FAILED**：未來若有「外部 API 已呼叫但 DB 寫入失敗」的情境
- ⚠️ **審查意見未提及**：若 Task 2.1（移除 Check-then-Act）先於 Task 1.1 執行，`DuplicateEmail` 會因為 `SaveChanges` 例外而走錯誤路徑，反而造成新 bug。**兩個 task 的執行順序至關重要。**

推薦的統一解法（`IsRetryable` + `HttpContext.Items`）優於「error code 白名單」，原因：
1. 業務語意由業務層（Handler/Repository）決定，而非 Filter
2. 不需要在 Filter 中 hard-code 特定 error code
3. 未來新增業務錯誤時，只需在建立 `Failure` 時設定 `IsRetryable`，不需修改 Filter
