using Microsoft.AspNetCore.Mvc;

namespace Lab.Swashbuckle.AspNetCore6.Controllers.Employee.v1_0;

[ApiVersion("1.0", Deprecated = true)]
[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DemoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return this.Ok(new
        {
            Version = 1.0,
            Name = "1.0"
        });
    }
}