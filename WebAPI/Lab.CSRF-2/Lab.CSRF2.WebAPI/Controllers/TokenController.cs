using Microsoft.AspNetCore.Mvc;
using Lab.CSRF2.WebAPI.Services;

namespace Lab.CSRF2.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var token = _tokenService.GenerateToken(maxUsage, expirationMinutes);
        Response.Headers["X-CSRF-Token"] = token;
        return Ok(new { message = "Token generated successfully", token });
    }
}
