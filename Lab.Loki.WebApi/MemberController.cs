using Microsoft.AspNetCore.Mvc;

namespace Lab.Loki.WebApi;

[ApiController]
public class MemberV1Controller(ILogger<MemberV1Controller> log) : ControllerBase
{
    private ILogger<MemberV1Controller> _log = log;
    [HttpGet]
    [Route("api/v1/members", Name = "GetMember")]
    public async Task<ActionResult> GetMemberCursor(
        CancellationToken cancel = default)
    {
        log.LogInformation("OKK");
        log.LogInformation(
            "API Request completed {@RequestDetails}",
            new { 
                Path = Request.Path,
                Method = Request.Method,
                StatusCode = Response.StatusCode,
                // Duration = stopwatch.ElapsedMilliseconds
            });
        return this.Ok();
    }
}