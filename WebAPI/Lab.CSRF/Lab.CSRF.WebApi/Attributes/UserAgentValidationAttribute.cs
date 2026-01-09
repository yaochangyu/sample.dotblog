using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;

namespace Lab.CSRF.WebApi.Attributes;

public class UserAgentValidationAttribute : ActionFilterAttribute
{
    // 白名單：主流瀏覽器的 User-Agent 模式
    private readonly Regex[] _allowedUserAgentPatterns = new[]
    {
        // Chrome/Chromium based browsers
        new Regex(@"Chrome/[\d\.]+", RegexOptions.IgnoreCase),
        new Regex(@"Chromium/[\d\.]+", RegexOptions.IgnoreCase),
        new Regex(@"Edg/[\d\.]+", RegexOptions.IgnoreCase), // Edge
        
        // Firefox
        new Regex(@"Firefox/[\d\.]+", RegexOptions.IgnoreCase),
        
        // Safari
        new Regex(@"Safari/[\d\.]+", RegexOptions.IgnoreCase),
        new Regex(@"AppleWebKit/[\d\.]+", RegexOptions.IgnoreCase),
        
        // Opera
        new Regex(@"OPR/[\d\.]+", RegexOptions.IgnoreCase),
        new Regex(@"Opera/[\d\.]+", RegexOptions.IgnoreCase),
        
        // Mobile browsers
        new Regex(@"Mobile Safari/[\d\.]+", RegexOptions.IgnoreCase),
        new Regex(@"CriOS/[\d\.]+", RegexOptions.IgnoreCase), // Chrome iOS
        new Regex(@"FxiOS/[\d\.]+", RegexOptions.IgnoreCase), // Firefox iOS
    };

    // 黑名單：已知的爬蟲和自動化工具特徵
    private readonly string[] _blockedKeywords = new[]
    {
        "python-requests", "curl", "wget", "scrapy", "bot", "crawler", 
        "spider", "postman", "insomnia", "httpie", "axios", "okhttp",
        "java", "go-http", "perl", "ruby", "php"
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

        if (userAgent.Length < 20)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "User-Agent 格式異常（長度過短），請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        // 先檢查黑名單（快速排除明顯的爬蟲）
        var containsBlockedKeyword = _blockedKeywords.Any(blocked => 
            userAgent.Contains(blocked, StringComparison.OrdinalIgnoreCase));

        if (containsBlockedKeyword)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = $"User-Agent 包含不允許的關鍵字，請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        // 檢查白名單（必須符合已知瀏覽器模式）
        var matchesAllowedPattern = _allowedUserAgentPatterns.Any(pattern => 
            pattern.IsMatch(userAgent));

        if (!matchesAllowedPattern)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "User-Agent 不符合已知瀏覽器模式，請求被拒絕"
            })
            {
                StatusCode = 403
            };
            return;
        }

        // 額外檢查：瀏覽器特徵必須包含作業系統資訊
        var hasOSInfo = userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                        userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase);

        if (!hasOSInfo)
        {
            context.Result = new JsonResult(new
            {
                success = false,
                message = "User-Agent 缺少作業系統資訊，請求被拒絕"
            })
            {
                StatusCode = 403
            };
        }
    }
}
