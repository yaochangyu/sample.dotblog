using Microsoft.AspNetCore.Authorization;
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

    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        return this.Ok("好");
    }
}