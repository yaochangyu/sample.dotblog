using Lab.CSRF2.WebAPI.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.CSRF2.WebAPI.Filters;

public class ValidateTokenAttribute : ActionFilterAttribute
{
    // 允許的來源白名單
    private static readonly string[] AllowedOrigins = new[]
    {
        "http://localhost:5073",
        "https://localhost:5073",
        "http://localhost:7001",
        "https://localhost:7001"
    };

    // 爬蟲 User-Agent 黑名單
    private static readonly string[] BotUserAgents = new[]
    {
        "curl/", "wget/", "scrapy", "python-requests", "java/", 
        "go-http-client", "http.rb/", "axios/", "node-fetch"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<ValidateTokenAttribute>>();

        var userAgent = request.Headers["User-Agent"].ToString();
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // 1. 驗證 User-Agent 黑名單
        if (!ValidateUserAgent(userAgent, logger))
        {
            context.Result = new ObjectResult(new { error = "Forbidden User-Agent" })
            {
                StatusCode = 403
            };
            return;
        }

        // 2. 驗證 Referer Header
        if (!ValidateReferer(request, logger))
        {
            context.Result = new ObjectResult(new { error = "Invalid or missing Referer header" })
            {
                StatusCode = 403
            };
            return;
        }

        // 3. 驗證 Origin Header (針對 CORS 請求)
        if (!ValidateOrigin(request, logger))
        {
            context.Result = new ObjectResult(new { error = "Invalid or missing Origin header" })
            {
                StatusCode = 403
            };
            return;
        }

        // 4. 驗證 Token
        var tokenService = context.HttpContext.RequestServices
            .GetRequiredService<ITokenProvider>();
        
        if (!request.Headers.TryGetValue("X-CSRF-Token", out var tokenValues))
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

        if (!tokenService.ValidateToken(token, userAgent, ipAddress))
        {
            logger.LogWarning("Token validation failed. Token: {Token}, UA: {UserAgent}, IP: {IP}, Time: {Time}",
                token?.Substring(0, Math.Min(8, token.Length)) + "...", userAgent, ipAddress, DateTime.UtcNow);
            
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or expired token" });
            return;
        }

        base.OnActionExecuting(context);
    }

    private bool ValidateUserAgent(string userAgent, ILogger logger)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            logger.LogWarning("Request without User-Agent header");
            return false;
        }

        var lowerUA = userAgent.ToLower();
        var isBot = BotUserAgents.Any(bot => lowerUA.Contains(bot));

        if (isBot)
        {
            logger.LogWarning("Bot User-Agent detected: {UA}", userAgent);
            return false;
        }

        return true;
    }

    private bool ValidateReferer(HttpRequest request, ILogger logger)
    {
        var referer = request.Headers["Referer"].ToString();
        
        // 開發環境: 允許無 Referer (例如 Postman 測試)
        // 生產環境: 建議設為必要
        if (string.IsNullOrEmpty(referer))
        {
            logger.LogWarning("Request without Referer header from {IP}", 
                request.HttpContext.Connection.RemoteIpAddress);
            // 開發環境暫時允許，生產環境改為 return false
            return true;
        }

        // 檢查 Referer 是否在白名單
        var isValid = AllowedOrigins.Any(origin => 
            referer.StartsWith(origin, StringComparison.OrdinalIgnoreCase));

        if (!isValid)
        {
            logger.LogWarning("Invalid Referer: {Referer} from {IP}", 
                referer, request.HttpContext.Connection.RemoteIpAddress);
        }

        return isValid;
    }

    private bool ValidateOrigin(HttpRequest request, ILogger logger)
    {
        // 只有 CORS 請求才會有 Origin Header
        if (!request.Headers.ContainsKey("Origin"))
        {
            return true; // 非 CORS 請求，跳過檢查
        }

        var origin = request.Headers["Origin"].ToString();
        
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        var isValid = AllowedOrigins.Any(allowed => 
            origin.Equals(allowed, StringComparison.OrdinalIgnoreCase));

        if (!isValid)
        {
            logger.LogWarning("Invalid Origin: {Origin} from {IP}", 
                origin, request.HttpContext.Connection.RemoteIpAddress);
        }

        return isValid;
    }
}
