using Microsoft.AspNetCore.Mvc;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Controllers;

[ApiController]
[Route("[controller]")]
public class ProtectController : ControllerBase
{
    private readonly ILogger<ProtectController> _logger;

    public ProtectController(ILogger<ProtectController> logger)
    {
        this._logger = logger;
    }

    // [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IActionResult> Get()
    {
        return this.Ok();
    }
}