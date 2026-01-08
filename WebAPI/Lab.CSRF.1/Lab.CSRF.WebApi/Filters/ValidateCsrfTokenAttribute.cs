using Lab.CSRF.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.CSRF.WebApi.Filters;

/// <summary>
/// CSRF Token 驗證 Filter
/// </summary>
public class ValidateCsrfTokenAttribute : ActionFilterAttribute
{
    private const string CsrfTokenHeaderName = "X-CSRF-Token";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var httpMethod = context.HttpContext.Request.Method;

        // 只有需要寫入的操作才需要驗證 CSRF Token
        if (httpMethod is "GET" or "HEAD" or "OPTIONS" or "TRACE")
        {
            base.OnActionExecuting(context);
            return;
        }

        var csrfTokenService = context.HttpContext.RequestServices.GetRequiredService<ICsrfTokenService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateCsrfTokenAttribute>>();

        // 從 Header 中取得 Token
        if (!context.HttpContext.Request.Headers.TryGetValue(CsrfTokenHeaderName, out var tokenValue))
        {
            logger.LogWarning("請求缺少 CSRF Token Header");
            context.Result = new ObjectResult(new { error = "缺少 CSRF Token" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        var token = tokenValue.ToString();

        // 驗證 Token
        if (!csrfTokenService.ValidateToken(token))
        {
            logger.LogWarning("CSRF Token 驗證失敗: {Token}", token);
            context.Result = new ObjectResult(new { error = "無效的 CSRF Token" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        logger.LogInformation("CSRF Token 驗證成功");
        base.OnActionExecuting(context);
    }
}
