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

namespace Lab.Idempotent.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class IdempotentAttribute : Attribute, IFilterFactory
{
    private readonly int? _expireSeconds;

    public IdempotentAttribute(int expireSeconds)
    {
        _expireSeconds = expireSeconds;
    }

    public IdempotentAttribute()
    {
    }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
        var defaultOptions = serviceProvider.GetRequiredService<HybridCacheEntryOptions>();
        var jsonOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
        var logger = serviceProvider.GetRequiredService<ILogger<IdempotentAttributeFilter>>();

        var cacheOptions = _expireSeconds.HasValue
            ? new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(_expireSeconds.Value) }
            : defaultOptions;

        return new IdempotentAttributeFilter(hybridCache, cacheOptions, jsonOptions, logger);
    }
}

public class IdempotentAttributeFilter : IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<IdempotentAttributeFilter> _logger;

    public IdempotentAttributeFilter(
        HybridCache hybridCache,
        HybridCacheEntryOptions cacheOptions,
        JsonSerializerOptions jsonOptions,
        ILogger<IdempotentAttributeFilter> logger)
    {
        _hybridCache = hybridCache;
        _cacheOptions = cacheOptions;
        _jsonOptions = jsonOptions;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var idempotencyKeyValues) ||
            string.IsNullOrWhiteSpace(idempotencyKeyValues.ToString()))
        {
            context.Result = Failure.Results[FailureCode.NotFoundIdempotentKey];
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString();
        var cacheKey = GetCacheKey(context.HttpContext, idempotencyKey);
        var fingerprint = ComputeFingerprint(context.ActionArguments);

        bool nextInvoked = false;
        ActionExecutedContext? executedContext = null;
        try
        {
            var cached = await _hybridCache.GetOrCreateAsync(
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
                        JsonSerializer.Serialize(objectResult.Value, _jsonOptions),
                        context.HttpContext.Response.Headers
                            .Where(h => !h.Key.StartsWith(':'))
                            .ToDictionary(h => h.Key, h => h.Value.Select(v => v ?? string.Empty).ToArray()),
                        fingerprint
                    );
                },
                _cacheOptions
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
            _logger.LogError(ex, "Idempotency cache operation failed for key {CacheKey}. Falling through to direct action execution.", cacheKey);

            if (!nextInvoked)
            {
                executedContext = await next();
                if (executedContext.Exception != null && !executedContext.ExceptionHandled)
                    ExceptionDispatchInfo.Throw(executedContext.Exception);
                context.Result = executedContext.Result;
            }
        }
    }

    private string ComputeFingerprint(IDictionary<string, object?> actionArguments)
    {
        var json = JsonSerializer.Serialize(actionArguments, _jsonOptions);
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
