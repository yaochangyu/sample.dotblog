using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Hybrid;

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

        var cacheOptions = _expireSeconds.HasValue
            ? new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(_expireSeconds.Value) }
            : defaultOptions;

        return new IdempotentAttributeFilter(hybridCache, cacheOptions, jsonOptions);
    }
}

public class IdempotentAttributeFilter : IAsyncActionFilter
{
    public const string HeaderName = "IdempotencyKey";

    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdempotentAttributeFilter(HybridCache hybridCache, HybridCacheEntryOptions cacheOptions, JsonSerializerOptions jsonOptions)
    {
        _hybridCache = hybridCache;
        _cacheOptions = cacheOptions;
        _jsonOptions = jsonOptions;
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

        bool nextInvoked = false;
        try
        {
            var cached = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    nextInvoked = true;
                    var executedContext = await next();

                    // Action 拋出未處理的例外，保留原始 stack trace 並重新拋出
                    if (executedContext.Exception != null && !executedContext.ExceptionHandled)
                    {
                        ExceptionDispatchInfo.Throw(executedContext.Exception);
                    }

                    if (executedContext.Result is not ObjectResult objectResult ||
                        objectResult.StatusCode is not (>= 200 and <= 299))
                    {
                        // 非 2xx 不快取，讓後續請求可重試
                        throw new IdempotentNonSuccessException();
                    }

                    return new IdempotentCacheEntry(
                        objectResult.StatusCode!.Value,
                        JsonSerializer.Serialize(objectResult.Value, _jsonOptions),
                        context.HttpContext.Response.Headers
                            .Where(h => !h.Key.StartsWith(':'))
                            .ToDictionary(h => h.Key, h => h.Value.Select(v => v ?? string.Empty).ToArray())
                    );
                },
                _cacheOptions
            );

            if (!nextInvoked)
            {
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
            // Action 已執行且回傳非 200，讓結果自然流出，不做任何處理
        }
    }

    private static string GetCacheKey(HttpContext httpContext, string idempotencyKey)
    {
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;
        return $"Idempotent:{method}:{path}:{idempotencyKey}";
    }
}

public record IdempotentCacheEntry(int StatusCode, string DataJson, Dictionary<string, string[]> Headers);

file sealed class IdempotentNonSuccessException : Exception;
