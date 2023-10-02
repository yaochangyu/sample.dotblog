using Lab.Context.Trace.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Context.Trace.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ILogger<DemoController> _logger;
    private readonly IObjectContextGetter<TraceContext> _contextGetter;

    public DemoController(ILogger<DemoController> logger,
        IObjectContextGetter<TraceContext> contextGetter)
    {
        _logger = logger;
        this._contextGetter = contextGetter;
    }

    [HttpGet(Name = "GetDemo")]
    public ActionResult Get()
    {
        var traceContext = this._contextGetter.Get();
        var userId = traceContext.UserId;

        // 由 Context 取得 UserId
        var member = Member.GetFakeMembers().FirstOrDefault(p => p.UserId == userId);

        this._logger.LogInformation(2000, "found {@Data}", member);

        return this.Ok(member);
    }
}