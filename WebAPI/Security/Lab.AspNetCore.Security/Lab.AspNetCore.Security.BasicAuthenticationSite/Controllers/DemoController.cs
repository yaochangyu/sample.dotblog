using Microsoft.AspNetCore.Mvc;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;

    public DemoController(ILogger<DemoController> logger)
    {
        this._logger = logger;
    }

    public async Task<IActionResult> Get()
    {
        this._logger.LogDebug("訪問沒有授權的端點");
        Console.WriteLine("AAAAA");
        return this.Ok();
    }
}