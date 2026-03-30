using Lab.DeviceFingerprint.WebApi.Domain.Entities;
using Lab.DeviceFingerprint.WebApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab.DeviceFingerprint.WebApi.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (await db.Users.AnyAsync(u => u.Username == "admin", ct))
            return Ok(new { message = "已存在 admin 帳號，略過 seed。" });

        db.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Email = "admin@test.com",
        });

        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Seed 完成！帳號：admin / password123" });
    }
}
