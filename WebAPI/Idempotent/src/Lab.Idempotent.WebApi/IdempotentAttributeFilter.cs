using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Lab.Idempotent.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class IdempotentAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var distributedCache = (IDistributedCache)serviceProvider.GetService(typeof(IDistributedCache));

        var filter = new IdempotentAttributeFilter(distributedCache);
        return filter;
    }
}

public class IdempotentAttributeFilter : ActionFilterAttribute
{
    public const string HeaderName = "IdempotencyKey";
    public static readonly TimeSpan Expiration = new(0, 0, 60);

    private readonly IDistributedCache _distributedCache;
    private string _idempotencyKey;
    private bool _hasIdempotencyKey;

    public IdempotentAttributeFilter(IDistributedCache distributedCache)
    {
        this._distributedCache = distributedCache;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // 檢查 Header 有沒有 IdempotencyKey
        if (context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var idempotencyKey) == false)
        {
            // 沒有的話則回傳 Bad Request
            context.Result = Failure.Results[FailureCode.NotFoundIdempotentKey];
            return;
        }

        this._idempotencyKey = idempotencyKey;
        
        var cacheData = this._distributedCache.GetString(this.GetDistributedCacheKey());
        if (cacheData == null)
        {
            // 沒有快取則進入 Action
            return;
        }

        // 從快取取出內容回傳給調用端 
        var jsonObject = JsonObject.Parse(cacheData);
        context.Result = new ObjectResult(jsonObject["Data"])
        {
            StatusCode = jsonObject["StatusCode"].GetValue<int>()
        };
        this._hasIdempotencyKey = true;
    }

    public override void OnResultExecuted(ResultExecutedContext context)
    {
        if (this._hasIdempotencyKey)
        {
            return;
        }

        var contextResult = (ObjectResult)context.Result;
        if (contextResult.StatusCode != (int)HttpStatusCode.OK)
        {
            return;
        }

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Expiration
        };
        var json = JsonSerializer.Serialize(new
        {
            Data = contextResult.Value,
            contextResult.StatusCode
        });
        this._distributedCache.SetString(this.GetDistributedCacheKey(),
            json,
            cacheOptions);
    }

    private string GetDistributedCacheKey()
    {
        return "IdempotencyKey:" + this._idempotencyKey;
    }
}
