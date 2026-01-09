using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.CSRF.WebApi.Attributes;

public class UserAgentValidationAttribute : ActionFilterAttribute
{
    private readonly string[] _blockedUserAgents = new[]
    {
        "python", "curl", "wget", "scrapy", "bot", "crawler", 
        "spider", "postman", "insomnia", "httpie"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();

        if (string.IsNullOrEmpty(userAgent))
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "缺少 User-Agent Header，請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        if (userAgent.Length < 10)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "User-Agent 長度過短，請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        var isBlocked = _blockedUserAgents.Any(blocked => 
            userAgent.Contains(blocked, StringComparison.OrdinalIgnoreCase));

        if (isBlocked)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = $"User-Agent 在阻擋清單中: {userAgent}"
            })
            {
                StatusCode = 403
            };
        }
    }
}
