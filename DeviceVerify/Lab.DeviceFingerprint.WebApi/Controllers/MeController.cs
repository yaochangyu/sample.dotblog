using System.Security.Claims;
using Lab.DeviceFingerprint.WebApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab.DeviceFingerprint.WebApi.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? User.FindFirstValue("sub")!);

        var user = await db.Users
            .Include(u => u.Devices.Where(d => d.IsVerified))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            Devices = user.Devices.Select(d => new
            {
                d.Id,
                d.DeviceName,
                d.UserAgent,
                d.CreatedAt,
                d.LastSeenAt,
            }),
        });
    }
}
