using AuthServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController(AccessTokenStore tokens) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var token = ExtractBearerToken();
        if (token is null)
            return Unauthorized("缺少 Authorization Bearer Token");

        var username = tokens.GetUsername(token);
        if (username is null)
            return Unauthorized("Token 無效或已過期");

        return Ok(new
        {
            username,
            message = $"哈囉，{username}！你已通過身分驗證。",
            issuedAt = DateTime.UtcNow
        });
    }

    private string? ExtractBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header["Bearer ".Length..].Trim();
        return null;
    }
}
