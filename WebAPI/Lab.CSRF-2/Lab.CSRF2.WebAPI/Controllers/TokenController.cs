using Lab.CSRF2.WebAPI.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lab.CSRF2.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("token")]
public class TokenController : ControllerBase
{
    private readonly ITokenProvider _tokenProvider;

    public TokenController(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    [HttpGet]
    public IActionResult GetToken([FromQuery] int maxUsage = 1, [FromQuery] int expirationMinutes = 5)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        
        // 檢查 User-Agent 是否存在
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return BadRequest(new { error = "User-Agent header is required" });
        }
        
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        var token = _tokenProvider.GenerateToken(maxUsage, expirationMinutes, userAgent, ipAddress);
        Response.Headers["X-CSRF-Token"] = token;
        return Ok(new { message = "Token generated successfully", token });
    }
}
