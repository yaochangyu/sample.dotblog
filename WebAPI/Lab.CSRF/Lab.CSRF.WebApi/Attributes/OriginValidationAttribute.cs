using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.CSRF.WebApi.Attributes;

public class OriginValidationAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedOrigins = new[]
    {
        "http://localhost:5173",
        "https://localhost:5173",
        "http://localhost:5073",
        "https://localhost:5073"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var origin = context.HttpContext.Request.Headers["Origin"].ToString();
        var referer = context.HttpContext.Request.Headers["Referer"].ToString();

        // Origin 和 Referer 至少一個必須存在
        if (string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(referer))
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "缺少 Origin 或 Referer Header,請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        // 驗證 Origin (優先)
        if (!string.IsNullOrEmpty(origin))
        {
            var isOriginAllowed = _allowedOrigins.Any(allowed =>
                origin.Equals(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isOriginAllowed)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = $"Origin 不在允許清單中: {origin}"
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }

        // 驗證 Referer (次要)
        if (!string.IsNullOrEmpty(referer))
        {
            var isRefererAllowed = _allowedOrigins.Any(allowed =>
                referer.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isRefererAllowed)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = $"Referer 不在允許清單中: {referer}"
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }
    }
}
