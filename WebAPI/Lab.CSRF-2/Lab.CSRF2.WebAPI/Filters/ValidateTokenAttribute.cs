using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Lab.CSRF2.WebAPI.Services;

namespace Lab.CSRF2.WebAPI.Filters;

public class ValidateTokenAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
        
        if (!context.HttpContext.Request.Headers.TryGetValue("X-CSRF-Token", out var tokenValues))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing X-CSRF-Token header" });
            return;
        }

        var token = tokenValues.FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Empty X-CSRF-Token header" });
            return;
        }

        if (!tokenService.ValidateToken(token))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
            return;
        }

        base.OnActionExecuting(context);
    }
}
