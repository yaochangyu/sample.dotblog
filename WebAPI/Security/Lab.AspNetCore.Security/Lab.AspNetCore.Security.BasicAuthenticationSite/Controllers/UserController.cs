using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get()
    {
        return this.Ok(new User
        {
            Name = this.User.Identity.Name,
            Age = 18
        });
    }
}