using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Lab.Idempotent.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
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
    private readonly IDistributedCache _distributedCache;
    private bool _isIdempotencyCache = false;
    const string HeaderName = "IdempotencyKey";
    private string _idempotencyKey;

    public IdempotentAttributeFilter(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var idempotencyKey) == false)
        {
            context.Result = new ObjectResult(new
            {
                ErrorCode = "NotFoundIdempotentKey",
                ErrorMessage = "Not found Idempotent key in header",
                Data = new
                {
                    PropertyName = "IdempotentKey",
                    Value = ""
                }
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            };

            return;
        }

        this._idempotencyKey = idempotencyKey.ToString();

        var cacheData = this._distributedCache.GetString(this.GetDistributedCacheKey());
        if (cacheData == null)
        {
            return;
        }

        context.Result = JsonSerializer.Deserialize<ObjectResult>(cacheData);
        this._isIdempotencyCache = true;
    }

    public override void OnResultExecuted(ResultExecutedContext context)
    {
        if (_isIdempotencyCache)
        {
            return;
        }

        var contextResult = context.Result;

        DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions();
        cacheOptions.AbsoluteExpirationRelativeToNow = new TimeSpan(24, 0, 0);

        _distributedCache.SetString(GetDistributedCacheKey(), JsonSerializer.Serialize(contextResult), cacheOptions);
    }

    private string GetDistributedCacheKey()
    {
        return "Idempotency:" + _idempotencyKey;
    }
}
