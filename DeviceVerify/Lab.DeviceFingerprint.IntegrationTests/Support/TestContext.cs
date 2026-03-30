using System.Security.Cryptography;
using System.Text;
using Lab.DeviceFingerprint.WebApi.Application.Services;
using Lab.DeviceFingerprint.WebApi.Domain.Entities;
using Lab.DeviceFingerprint.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lab.DeviceFingerprint.IntegrationTests.Support;

public class FixedOtpGenerator(string otp) : IOtpGenerator
{
    public string Generate() => otp;
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();
    public const string FixedOtp = "888888";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext)
                         || (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                         || d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true
                         || d.ServiceType == typeof(IOtpGenerator)
                         || d.ServiceType == typeof(IDistributedCache))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // 不設 L2 Redis，HybridCache 僅使用 L1 記憶體
            services.AddDistributedMemoryCache();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
            services.AddSingleton<IOtpGenerator>(new FixedOtpGenerator(FixedOtp));
        });

        builder.UseEnvironment("Development");
    }
}

public class TestContext
{
    public TestWebApplicationFactory Factory { get; } = new();
    public HttpClient Client => Factory.CreateClient();

    public async Task SeedUserAsync(string username, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync(u => u.Username == username)) return;

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = $"{username}@test.com",
        });
        await db.SaveChangesAsync();
    }

    public async Task SeedVerifiedDeviceAsync(string username, string fingerprint)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Username == username);
        var hash = HashFingerprint(fingerprint);

        if (await db.UserDevices.AnyAsync(d => d.UserId == user.Id && d.FingerprintHash == hash)) return;

        db.UserDevices.Add(new UserDevice
        {
            UserId = user.Id,
            FingerprintHash = hash,
            DeviceName = "Test Device",
            UserAgent = "TestAgent",
            IsVerified = true,
        });
        await db.SaveChangesAsync();
    }

    public static string HashFingerprint(string fingerprint)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
