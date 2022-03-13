using Microsoft.AspNetCore.Mvc;

namespace Lab.Swashbuckle.AspNetCore6.Controllers.Employee.v1_1;

[ApiVersion("1.1")]
[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return this.Ok(new
        {
            Version = 1.1,
            Name = "1.1"
        });
    }
}