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
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
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
