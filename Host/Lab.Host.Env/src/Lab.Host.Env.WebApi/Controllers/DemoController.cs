using Lab.Host.Env.WebApi.ServiceModels;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Host.Env.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private IWebHostEnvironment _host;
    private readonly ILogger<DemoController> _logger;

    public DemoController(ILogger<DemoController> logger, IWebHostEnvironment host)
    {
        this._logger = logger;
        this._host = host;
    }

    [HttpGet]
    public async Task<ActionResult<EnvironmentResponse>> Get(CancellationToken cancel = default)
    {
        return this.Ok(new
        {
            this._host.ApplicationName,
            this._host.EnvironmentName
        });
    }
}