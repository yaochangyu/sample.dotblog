using Lab.DeviceFingerprint.WebApi.Application.DTOs;
using Lab.DeviceFingerprint.WebApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab.DeviceFingerprint.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var result = await authService.LoginAsync(request, userAgent, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("verify-device")]
    public async Task<IActionResult> VerifyDevice([FromBody] VerifyDeviceRequest request, CancellationToken ct)
    {
        try
        {
            var userAgent = request.UserAgent ?? Request.Headers.UserAgent.ToString();
            var req = request with { UserAgent = userAgent };
            var result = await authService.VerifyDeviceAsync(req, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
