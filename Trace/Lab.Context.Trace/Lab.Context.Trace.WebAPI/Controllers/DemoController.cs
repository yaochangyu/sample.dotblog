using Lab.Context.Trace.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Context.Trace.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;
    private readonly IContextGetter<AuthContext?> _authContextGetter;

    public DemoController(ILogger<DemoController> logger,
        IContextGetter<AuthContext?> authContextGetter)
    {
        this._logger = logger;
        this._authContextGetter = authContextGetter;
    }

    [HttpGet(Name = "GetDemo")]
    public ActionResult Get()
    {
        var authContext = this._authContextGetter.Get();
        var userId = authContext.UserId;
        // 由 Context 取得 UserId
        var member = Member.GetFakeMembers().FirstOrDefault(p => p.UserId == userId);

        this._logger.LogInformation(2000, "found {@Data}", member);

        return this.Ok(member);
    }
}