using System.Net;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.Idempotent.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    private readonly int? _expireSeconds;

    public IdempotentAttribute()
    {
    }

    public IdempotentAttribute(int expireSeconds)
    {
        _expireSeconds = expireSeconds;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var hybridCache = services.GetRequiredService<HybridCache>();
        var defaultOptions = services.GetRequiredService<HybridCacheEntryOptions>();
        var jsonOptions = services.GetRequiredService<JsonSerializerOptions>();
        var logger = services.GetRequiredService<ILogger<IdempotentAttribute>>();

        var cacheOptions = _expireSeconds.HasValue
            ? new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(_expireSeconds.Value) }
            : defaultOptions;

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var idempotencyKeyValues) ||
            string.IsNullOrWhiteSpace(idempotencyKeyValues.ToString()))
        {
            context.Result = Failure.Results[FailureCode.NotFoundIdempotentKey];
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString();
        var cacheKey = GetCacheKey(context.HttpContext, idempotencyKey);
        var fingerprint = ComputeFingerprint(context.ActionArguments, jsonOptions);

        bool nextInvoked = false;
        ActionExecutedContext? executedContext = null;

        // NOTE: HybridCache 提供單一 instance 內的 stampede protection（相同 key 只執行一次 factory）。
        // 在多節點（多 pod）情境下，不同節點仍可能並發執行 factory，導致重複副作用。
        // 若需跨節點的並發保護，應在此加入分散式鎖（例如 Redlock），並在原始請求處理期間
        // 對未完成的重試回傳 HTTP 409 Conflict。
        try
        {
            var cached = await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    nextInvoked = true;
                    executedContext = await next();

                    // Action 拋出未處理的例外，保留原始 stack trace 並重新拋出
                    if (executedContext.Exception != null && !executedContext.ExceptionHandled)
                    {
                        ExceptionDispatchInfo.Throw(executedContext.Exception);
                    }

                    if (executedContext.Result is not ObjectResult objectResult)
                    {
                        // 非 ObjectResult 不快取
                        throw new IdempotentNonSuccessException();
                    }

                    // StatusCode 為 null 時 ASP.NET Core 預設回傳 200
                    var statusCode = objectResult.StatusCode ?? (int)HttpStatusCode.OK;
                    if (statusCode is not (>= 200 and <= 299))
                    {
                        // 非 2xx 不快取，讓後續請求可重試
                        throw new IdempotentNonSuccessException();
                    }

                    return new IdempotentCacheEntry(
                        statusCode,
                        JsonSerializer.Serialize(objectResult.Value, jsonOptions),
                        context.HttpContext.Response.Headers
                            .Where(h => !h.Key.StartsWith(':'))
                            .ToDictionary(h => h.Key, h => h.Value.Select(v => v ?? string.Empty).ToArray()),
                        fingerprint
                    );
                },
                cacheOptions
            );

            if (!nextInvoked)
            {
                // Cache hit：驗證 request payload 是否與原始請求一致
                if (cached.Fingerprint != fingerprint)
                {
                    context.Result = Failure.Results[FailureCode.IdempotentKeyPayloadMismatch];
                    return;
                }

                // Cache hit：還原自訂 response headers
                foreach (var (key, values) in cached.Headers)
                {
                    context.HttpContext.Response.Headers[key] = values;
                }

                // Cache hit：回傳快取的結果
                context.Result = new ObjectResult(JsonNode.Parse(cached.DataJson))
                {
                    StatusCode = cached.StatusCode
                };
            }
        }
        catch (IdempotentNonSuccessException)
        {
            // Action 已執行但回傳非 2xx，明確透傳 action 的結果，不快取
            if (executedContext?.Result != null)
                context.Result = executedContext.Result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cache 基礎設施失敗（Redis 連線中斷等），記錄錯誤後 fallthrough 直接執行 action
            logger.LogError(ex, "Idempotency cache operation failed for key {CacheKey}. Falling through to direct action execution.", cacheKey);

            if (!nextInvoked)
            {
                executedContext = await next();
                if (executedContext.Exception != null && !executedContext.ExceptionHandled)
                    ExceptionDispatchInfo.Throw(executedContext.Exception);
                context.Result = executedContext.Result;
            }
        }
    }

    private static string ComputeFingerprint(IDictionary<string, object?> actionArguments, JsonSerializerOptions jsonOptions)
    {
        var json = JsonSerializer.Serialize(actionArguments, jsonOptions);
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private static string GetCacheKey(HttpContext httpContext, string idempotencyKey)
    {
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var queryString = httpContext.Request.QueryString.Value ?? string.Empty;
        return $"Idempotent:{method}:{path}{queryString}:{idempotencyKey}";
    }
}

public record IdempotentCacheEntry(int StatusCode, string DataJson, Dictionary<string, string[]> Headers, string Fingerprint);

file sealed class IdempotentNonSuccessException : Exception;
