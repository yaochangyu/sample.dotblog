using Microsoft.AspNetCore.Mvc;
using Lab.CSRF2.WebAPI.Filters;

namespace Lab.CSRF2.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProtectedController : ControllerBase
{
    [HttpPost]
    [ValidateToken]
    public IActionResult PostData([FromBody] ProtectedRequest request)
    {
        return Ok(new 
        { 
            message = "Request processed successfully",
            receivedData = request.Data,
            timestamp = DateTime.UtcNow
        });
    }
}

public class ProtectedRequest
{
    public string Data { get; set; } = string.Empty;
}
