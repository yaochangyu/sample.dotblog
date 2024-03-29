using Lab.Swashbuckle.AspNetCore6.Examples;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Lab.Swashbuckle.AspNetCore6.Controllers;

[ApiController]
[Route("[controller]")]
public class EmployeeController : ControllerBase
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(ILogger<EmployeeController> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// 取得會員
    /// </summary>
    /// <param name="request"></param>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /Todo
    ///     {
    ///        "id": 1,
    ///        "name": "Item #1",
    ///        "isComplete": true
    ///     }
    /// </remarks>

    // [HttpGet(Name = "GetEmployee")]
    [HttpGet]
    [Produces("application/json")]
    // [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(200, "查詢結果", typeof(EmployeeResponse))]
    [SwaggerRequestExample(typeof(QueryEmployeeRequest), typeof(QueryEmployeeRequestExample))]
    [SwaggerResponseExample(200, typeof(EmployeeResponseExample))]
    public async Task<IActionResult> Get(QueryEmployeeRequest request)
    {
        if (this.ModelState.IsValid == false)
        {
            return this.BadRequest();
        }

        return this.Ok(new List<EmployeeResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "yao",
                Age = 20
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "小章",
                Age = 18,
                Remark = "說明"
            }
        });
    }
}