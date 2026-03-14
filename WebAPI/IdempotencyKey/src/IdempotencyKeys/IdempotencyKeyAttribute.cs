using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace IdempotencyKey.WebApi.IdempotencyKeys;

/// <summary>
/// 套用 Idempotency Key 保護的 Action Filter。
/// 可直接標註於 Controller action 上，透過 RequestServices 取得依賴。
/// </summary>
/// <example>
/// [HttpPost]
/// [IdempotencyKey]
/// public async Task&lt;ActionResult&lt;Member&gt;&gt; Create(CreateMemberRequest request) { ... }
///
/// // 自訂 TTL（小時）與是否強制要求 header
/// [IdempotencyKey(TtlHours = 48, Required = false)]
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IdempotencyKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string ReplayHeader = "X-Idempotent-Replay";

    /// <summary>Idempotency key 的保留時間（小時），預設 24 小時。</summary>
    public int TtlHours { get; set; } = 24;

    /// <summary>InProgress 狀態的鎖定時間（秒），系統崩潰後自動釋放。預設 30 秒。</summary>
    public int LockTtlSeconds { get; set; } = 30;

    /// <summary>若為 true，缺少 Idempotency-Key header 時回傳 400；否則略過冪等保護。預設 true。</summary>
    public bool Required { get; set; } = true;

    /// <summary>計算 fingerprint 時要排除的欄位名稱（大小寫不敏感），適合排除每次重試可能變動但不影響業務語意的欄位，例如 timestamp、nonce。</summary>
    /// <example>[IdempotencyKey(ExcludeFields = ["clientTimestamp", "requestNonce"])]</example>
    public string[] ExcludeFields { get; set; } = [];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var store = services.GetRequiredService<IIdempotencyKeyStore>();
        var logger = services.GetRequiredService<ILogger<IdempotencyKeyAttribute>>();

        // 只對非冪等的寫入操作套用保護
        var method = context.HttpContext.Request.Method;
        if (!HttpMethods.IsPost(method) && !HttpMethods.IsPatch(method))
        {
            await next();
            return;
        }

        var idempotencyKey = context.HttpContext.Request.Headers[IdempotencyKeyHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (Required)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    error = $"{IdempotencyKeyHeader} header is required"
                });
                return;
            }

            await next();
            return;
        }

        if (idempotencyKey.Length > 255)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = $"{IdempotencyKeyHeader} must not exceed 255 characters"
            });
            return;
        }

        var jsonOptions = services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        var fingerprint = ComputeFingerprint(context, ExcludeFields, jsonOptions);

        // [1] 原子操作嘗試取得鎖（SET NX），使用短 TTL 防止系統崩潰後鎖永久占用
        var existing = await store.TryAcquireAsync(idempotencyKey, fingerprint, TtlHours, LockTtlSeconds,
            context.HttpContext.RequestAborted);

        if (existing is not null)
        {
            // Key 已存在，依狀態處理
            HandleExistingKey(context, existing, fingerprint, logger);
            return;
        }

        // [2] 成功取得鎖（IN_PROGRESS），執行業務邏輯
        logger.LogInformation("Idempotency key {Key} acquired, executing action", idempotencyKey);

        var executedContext = await next();

        await HandleActionResult(executedContext, idempotencyKey, TtlHours, store, logger, context.HttpContext.RequestAborted);
    }

    private static void HandleExistingKey(
        ActionExecutingContext context, IdempotencyKeyRecord existing, string fingerprint,
        ILogger logger)
    {
        var key = existing.Key;

        switch (existing.Status)
        {
            case IdempotencyKeyStatus.InProgress:
                logger.LogInformation("Idempotency key {Key} is IN_PROGRESS, rejecting concurrent request", key);
                context.Result = new ConflictObjectResult(new
                {
                    error = "A request with this idempotency key is already being processed. Retry after the original request completes."
                });
                return;

            case IdempotencyKeyStatus.Completed:
            case IdempotencyKeyStatus.Failed:
                // [3] 驗證 request fingerprint，防止相同 key 搭配不同 payload
                if (existing.RequestFingerprint != fingerprint)
                {
                    logger.LogWarning(
                        "Idempotency key {Key} fingerprint mismatch. Stored: {Stored}, Received: {Received}",
                        key, existing.RequestFingerprint, fingerprint);

                    context.Result = new UnprocessableEntityObjectResult(new
                    {
                        error = "Idempotency key has already been used with a different request payload."
                    });
                    return;
                }

                // [4] 重播快取回應
                logger.LogInformation("Replaying cached response for idempotency key {Key} (status: {Status})",
                    key, existing.Status);

                context.HttpContext.Response.Headers[ReplayHeader] = "true";
                context.Result = new ContentResult
                {
                    StatusCode = existing.ResponseStatusCode ?? 200,
                    Content = existing.ResponseBody,
                    ContentType = existing.ResponseContentType ?? "application/json"
                };
                return;
        }
    }

    private static async Task HandleActionResult(
        ActionExecutedContext executedContext, string idempotencyKey, int ttlHours,
        IIdempotencyKeyStore store, ILogger logger, CancellationToken ct)
    {
        // 若發生未處理的例外，刪除 key 讓客戶端可以重試
        if (executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            logger.LogWarning(executedContext.Exception,
                "Unhandled exception for idempotency key {Key}, deleting key to allow retry", idempotencyKey);
            await store.DeleteAsync(idempotencyKey, ct);
            return;
        }

        // 使用 ASP.NET Core 設定的 JSON 序列化選項（camelCase 等），確保重播 body 與原始回應一致
        var jsonOptions = executedContext.HttpContext.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;

        var (statusCode, body, contentType) = CaptureResult(executedContext.Result, jsonOptions);

        if (statusCode >= 500)
        {
            // 暫時性失敗：刪除 key，讓客戶端重試
            logger.LogWarning(
                "Transient failure (HTTP {StatusCode}) for idempotency key {Key}, deleting key to allow retry",
                statusCode, idempotencyKey);
            await store.DeleteAsync(idempotencyKey, ct);
            return;
        }

        // 無副作用的業務錯誤（如 DuplicateEmail、DbConcurrency）：刪除 key，讓客戶端修正後重試
        if (executedContext.HttpContext.Items.ContainsKey("Idempotency:ShouldDeleteKey"))
        {
            logger.LogInformation(
                "Retryable failure (HTTP {StatusCode}) for idempotency key {Key}, deleting key to allow retry",
                statusCode, idempotencyKey);
            await store.DeleteAsync(idempotencyKey, ct);
            return;
        }

        // 成功或確定性失敗（含副作用的業務邏輯錯誤）：快取回應
        if (statusCode >= 400)
        {
            logger.LogInformation("Caching failed response (HTTP {StatusCode}) for idempotency key {Key}",
                statusCode, idempotencyKey);
            await store.SetFailedAsync(idempotencyKey, statusCode, body, contentType, ttlHours, ct);
        }
        else
        {
            logger.LogInformation("Caching successful response (HTTP {StatusCode}) for idempotency key {Key}",
                statusCode, idempotencyKey);
            await store.SetCompletedAsync(idempotencyKey, statusCode, body, contentType, ttlHours, ct);
        }
    }

    /// <summary>使用 HTTP Method、Path 與 action arguments 計算 SHA-256 fingerprint，防止相同 key 跨端點誤判相符。</summary>
    private static string ComputeFingerprint(ActionExecutingContext context, string[] excludeFields, JsonSerializerOptions jsonOptions)
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

        var json = JsonSerializer.Serialize(input, jsonOptions);

        if (excludeFields.Length > 0)
        {
            var excluded = new HashSet<string>(excludeFields, StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(json);
            var filtered = FilterJsonElement(doc.RootElement, excluded);
            json = JsonSerializer.Serialize(filtered, jsonOptions);
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>遞迴過濾 JSON 物件中指定的欄位（含巢狀物件）。</summary>
    private static object? FilterJsonElement(JsonElement element, HashSet<string> excluded)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .Where(p => !excluded.Contains(p.Name))
                .OrderBy(p => p.Name)
                .ToDictionary(p => p.Name, p => FilterJsonElement(p.Value, excluded)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(e => FilterJsonElement(e, excluded))
                .ToArray(),
            _ => JsonSerializer.Deserialize<object>(element.GetRawText())
        };
    }

    private static (int statusCode, string? body, string? contentType) CaptureResult(
        IActionResult? result, JsonSerializerOptions? jsonOptions = null)
    {
        return result switch
        {
            ObjectResult objectResult => (
                objectResult.StatusCode ?? 200,
                JsonSerializer.Serialize(objectResult.Value, jsonOptions),
                "application/json"
            ),
            ContentResult contentResult => (
                contentResult.StatusCode ?? 200,
                contentResult.Content,
                contentResult.ContentType
            ),
            StatusCodeResult statusCodeResult => (
                statusCodeResult.StatusCode,
                null,
                null
            ),
            _ => (200, null, null)
        };
    }
}