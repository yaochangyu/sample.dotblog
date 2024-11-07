using Microsoft.AspNetCore.Mvc;

namespace Lab.Loki.WebApi;

[ApiController]
public class MemberV1Controller(ILogger<MemberV1Controller> Logger) : ControllerBase
{
    [HttpGet]
    [Route("api/v1/members", Name = "GetMember")]
    public async Task<ActionResult> GetMemberCursor(
        CancellationToken cancel = default)
    {
        Logger.LogInformation(new EventId(2000, "Trace"), "Start {ControllerName}.{MethodName}...",
                                    nameof(MemberV1Controller), nameof(GetMemberCursor));
        
        var sensorInput = new { Latitude = 25, Longitude = 134 };
        Logger.LogInformation("Processing {@SensorInput}", sensorInput);
        return this.Ok();
    }
}