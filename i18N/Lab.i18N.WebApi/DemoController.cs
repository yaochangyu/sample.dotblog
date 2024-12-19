using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Lab.i18N.WebApi;

[ApiController]
public class DemoController(IStringLocalizer<DemoController> localizer) : ControllerBase
{
    [HttpGet]
    [Route("api/v1/demo", Name = "GetDemo")]
    public IActionResult Get()
    {
        var desc = localizer["about.description"];

        return Ok(new
        {
            desc
        });
    }
}