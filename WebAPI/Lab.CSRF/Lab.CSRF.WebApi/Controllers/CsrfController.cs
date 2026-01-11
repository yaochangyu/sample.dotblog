using Lab.CSRF.WebApi.Attributes;
using Lab.CSRF.WebApi.Providers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Lab.CSRF.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsrfController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;
    private readonly ITokenNonceProvider _nonceProvider;

    public CsrfController(IAntiforgery antiforgery, ITokenNonceProvider nonceProvider)
    {
        _antiforgery = antiforgery;
        _nonceProvider = nonceProvider;
    }

    [HttpGet("token")]
    [IgnoreAntiforgeryToken]
    [OriginValidation]
    [UserAgentValidation]
    public async Task<IActionResult> GetToken()
    {
        // 使用 GetAndStoreTokens() 產生 Token 對
        // 這會自動產生 CookieToken (.AspNetCore.Antiforgery.XXX，HttpOnly=true)
        // 並設定 Cache-Control: no-cache, Pragma: no-cache, X-Frame-Options: SAMEORIGIN
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        
        // 手動加入 RequestToken Cookie (HttpOnly=false)，供前端 JavaScript 讀取
        // 參考: https://blog.darkthread.net/blog/spa-minapi-xsrf/
        Response.Cookies.Append(
            tokens.HeaderName!,  // 使用 HeaderName 作為 Cookie 名稱 (X-XSRF-TOKEN)
            tokens.RequestToken!, 
            new CookieOptions 
            { 
                HttpOnly = false,  // 允許 JavaScript 讀取
                SameSite = SameSiteMode.Strict,  // 防止跨站請求
                Secure = HttpContext.Request.IsHttps,  // HTTPS 時才設為 Secure
                Path = "/"
            });
        
        // 產生 Nonce（防止重放攻擊）
        var nonce = await _nonceProvider.GenerateNonceAsync();
        
        return Ok(new { 
            message = "CSRF Token 已設定在 Cookie 中",
            nonce = nonce
        });
    }

    [HttpPost("protected")]
    [ValidateAntiForgeryToken]
    [OriginValidation]
    [UserAgentValidation]
    public async Task<IActionResult> ProtectedAction(
        [FromBody] DataRequest request)
    {
        var nonce = Request.Headers["X-Nonce"].ToString();
        
        if (!await _nonceProvider.ValidateAndConsumeNonceAsync(nonce))
        {
            return BadRequest(new { 
                success = false, 
                message = "Nonce 無效或已使用（防止重放攻擊）" 
            });
        }

        return Ok(new { 
            success = true, 
            message = "CSRF 驗證成功！", 
            data = request.Data,
            timestamp = DateTime.Now 
        });
    }
}

public class DataRequest
{
    public string? Data { get; set; }
}
