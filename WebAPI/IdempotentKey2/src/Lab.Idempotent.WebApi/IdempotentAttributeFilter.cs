using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.Idempotent.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class IdempotentAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
        return new IdempotentAttributeFilter(hybridCache);
    }
}

public class IdempotentAttributeFilter : IAsyncActionFilter
{
    public const string HeaderName = "IdempotencyKey";

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(60)
    };

    private readonly HybridCache _hybridCache;

    public IdempotentAttributeFilter(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
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

                    if (executedContext.Result is not ObjectResult objectResult ||
                        objectResult.StatusCode != (int)HttpStatusCode.OK)
                    {
                        // 非 200 不快取，讓後續請求可重試
                        throw new IdempotentNonSuccessException();
                    }

                    return new IdempotentCacheEntry(
                        objectResult.StatusCode!.Value,
                        JsonSerializer.Serialize(objectResult.Value)
                    );
                },
                CacheOptions
            );

            if (!nextInvoked)
            {
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

public record IdempotentCacheEntry(int StatusCode, string DataJson);

file sealed class IdempotentNonSuccessException : Exception;
