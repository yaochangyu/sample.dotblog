using Microsoft.AspNetCore.Mvc;

namespace Lab.MsDI.Lazy.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;
    private readonly IService _service;
    public DemoController(ILogger<DemoController> logger, IService service)
    {
        this._logger = logger;
        this._service = service;
    }

    [HttpGet(Name = "GetDemo")]
    public ActionResult Get()
    {
        return this.Ok(this._service.Get());
    }
}