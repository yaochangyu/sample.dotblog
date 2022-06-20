using System.Threading.Tasks;
using Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest.Controllers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        this._logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return this.Ok("好");
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Post(User user)
    {
        return this.Ok("好");
    }
}