using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Lab.CSRF.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsrfController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public CsrfController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("token")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetToken()
    {
        // 方案 1: 驗證 Referer/Origin (必須來自本站)
        var referer = Request.Headers["Referer"].ToString();
        var origin = Request.Headers["Origin"].ToString();
        var allowedOrigins = new[] { "http://localhost:5073", "https://yourdomain.com" };

        if (!string.IsNullOrEmpty(origin))
        {
            if (!allowedOrigins.Any(x => origin.StartsWith(x)))
            {
                return BadRequest(new { error = "Invalid origin" });
            }
        }
        else if (!string.IsNullOrEmpty(referer))
        {
            if (!allowedOrigins.Any(x => referer.StartsWith(x)))
            {
                return BadRequest(new { error = "Invalid referer" });
            }
        }
        else
        {
            // 直接存取 API (不是從網頁發起) - 拒絕
            return BadRequest(new { error = "Missing origin/referer header" });
        }

        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("protected")]
    [ValidateAntiForgeryToken]
    [ServiceFilter(typeof(ValidateOriginAttribute))] // 雙重驗證
    public IActionResult ProtectedAction([FromBody] DataRequest request)
    {
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
