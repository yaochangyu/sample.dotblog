using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Lab.CSRF2.WebAPI.Services;

namespace Lab.CSRF2.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("token")]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpGet]
    public IActionResult GetToken([FromQuery] int maxUsage = 1, [FromQuery] int expirationMinutes = 5)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        var token = _tokenService.GenerateToken(maxUsage, expirationMinutes, userAgent, ipAddress);
        Response.Headers["X-CSRF-Token"] = token;
        return Ok(new { message = "Token generated successfully", token });
    }
}
