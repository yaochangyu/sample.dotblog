using System.Text.Json;
using System.Text.Json.Nodes;
using Lab.JsonMergePatch.Models;
using Microsoft.AspNetCore.Mvc;
using Morcatko.AspNetCore.JsonMergePatch;
using Swashbuckle.AspNetCore.Filters;

namespace Lab.JsonMergePatch.Controllers;

[ApiController]
[Route("[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly ILogger<EmployeeController> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public EmployeeController(ILogger<EmployeeController> logger,
        JsonSerializerOptions jsonSerializerOptions)
    {
        this._logger = logger;
        this._jsonSerializerOptions = jsonSerializerOptions;
    }

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        return this.Ok(this.GetEmployee());
    }

    [HttpPatch]
    [SwaggerRequestExample(typeof(PatchEmployeeRequest), typeof(PatchEmployeeRequest.PatchEmployeeRequestExample))]
    public async Task<ActionResult> Patch(JsonMergePatchDocument<PatchEmployeeRequest> request)
    {
        var original = this.GetEmployee();
        var patchResult = request.ApplyToT(original);
        return this.Ok(patchResult);
    }

    Employee GetEmployee()
    {
        var now = DateTimeOffset.Now;
        var userId = "Sys";
        return new Employee
        {
            Id = Guid.NewGuid(),
            Address = new Address
            {
                Address1 = "台北市",
                Address2 = "大安區",
                Street = "忠孝東路"
            },
            Birthday = new DateTime(2009, 12, 25),
            CreatedAt = now,
            CreatedBy = userId,
            ModifiedAt = now,
            ModifiedBy = userId,
        };
    }
}