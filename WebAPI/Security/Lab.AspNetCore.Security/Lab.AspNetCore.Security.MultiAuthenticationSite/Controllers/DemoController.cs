using AspNetCore.Authentication.ApiKey;
using Lab.AspNetCore.Security.BasicAuthentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lab.AspNetCore.Security.MultiAuthenticationSite.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;

    public DemoController(ILogger<DemoController> logger)
    {
        _logger = logger;
    }

    [Authorize(AuthenticationSchemes = BasicAuthenticationDefaults.AuthenticationScheme)] 
    [Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)] 

    public ActionResult Get()
    {
        return this.Ok("OK~好");
    }
}