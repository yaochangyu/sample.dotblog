using System.Net;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Lab.Idempotent.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);

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
        var provider = context.HttpContext.RequestServices;
        var hybridCache = provider.GetRequiredService<HybridCache>();
        var defaultOptions = provider.GetRequiredService<HybridCacheEntryOptions>();
        var jsonOptions = provider.GetRequiredService<JsonSerializerOptions>();
        var logger = provider.GetRequiredService<ILogger<IdempotentAttribute>>();

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
        var lockKey = $"{cacheKey}:lock";
        var lockToken = Guid.NewGuid().ToString("N");
        var lockExpiry = LockExpiry;
        var fingerprint = ComputeFingerprint(context.ActionArguments, jsonOptions);

        var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
        var db = connectionMultiplexer.GetDatabase();

        bool nextInvoked = false;
        ActionExecutedContext? executedContext = null;
        string? acquiredToken = null; // 持有鎖的 token，在 finally 用來釋放鎖

        try
        {
            var cached = await hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    // 分散式鎖：SETNX，只有第一個 pod 能成功取得鎖
                    // acquiredToken 提升到 try 外層，確保鎖在 HybridCache 寫入 L2 後才釋放
                    var lockAcquired = await db.StringSetAsync(lockKey, lockToken, lockExpiry, When.NotExists);
                    if (!lockAcquired)
                        throw new ConcurrentIdempotentRequestException();

                    acquiredToken = lockToken;

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
        catch (ConcurrentIdempotentRequestException)
        {
            // 同一個 idempotency key 正在被另一個 pod 處理中，回傳 409
            context.Result = Failure.Results[FailureCode.ConcurrentRequest];
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
        finally
        {
            // 釋放分散式鎖（此時 HybridCache 已完成 L2 寫入）
            // 使用 Lua script 確保原子性：只有持有鎖的 token 才能刪除
            if (acquiredToken != null)
            {
                try
                {
                    const string releaseLockScript = """
                        if redis.call('get', KEYS[1]) == ARGV[1] then
                            return redis.call('del', KEYS[1])
                        else
                            return 0
                        end
                        """;
                    await db.ScriptEvaluateAsync(releaseLockScript,
                        [(RedisKey)lockKey],
                        [(RedisValue)acquiredToken]);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to release idempotency lock for {LockKey}. It will expire automatically.", lockKey);
                }
            }
        }
    }

    private static string ComputeFingerprint(IDictionary<string, object?> actionArguments, JsonSerializerOptions jsonOptions)
    {
        // 排除 CancellationToken 等 framework 注入的非 payload 參數
        var payload = actionArguments
            .Where(kv => kv.Value is not CancellationToken)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var json = JsonSerializer.Serialize(payload, jsonOptions);
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

file sealed class ConcurrentIdempotentRequestException : Exception;
