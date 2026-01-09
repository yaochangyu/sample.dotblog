using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.CSRF.WebApi.Attributes;

public class RefererValidationAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedReferers = new[]
    {
        "http://localhost:5173",
        "https://localhost:5173",
        "http://localhost",
        "https://localhost"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var referer = context.HttpContext.Request.Headers["Referer"].ToString();

        if (string.IsNullOrEmpty(referer))
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "缺少 Referer Header，請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        var isAllowed = _allowedReferers.Any(allowed => 
            referer.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = $"Referer 不在允許清單中: {referer}"
            })
            {
                StatusCode = 403
            };
        }
    }
}
