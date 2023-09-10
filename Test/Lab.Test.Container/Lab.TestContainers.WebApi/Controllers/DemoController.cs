using Microsoft.AspNetCore.Mvc;

namespace Lab.Test.Container.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;

    public DemoController(ILogger<DemoController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetDemo")]
    public ActionResult Get()
    {
        return this.Ok("OK~");
    }
}