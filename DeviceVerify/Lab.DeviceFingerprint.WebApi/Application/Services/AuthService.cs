using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Lab.DeviceFingerprint.WebApi.Application.DTOs;
using Lab.DeviceFingerprint.WebApi.Domain.Entities;
using Lab.DeviceFingerprint.WebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;

namespace Lab.DeviceFingerprint.WebApi.Application.Services;

public class AuthService(
    AppDbContext db,
    HybridCache cache,
    IConfiguration config,
    ILogger<AuthService> logger) : IAuthService
{
    private static string HashFingerprint(string fingerprint)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string GenerateJwt(User user, string fingerprintHash)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(config.GetValue<int>("Jwt:ExpiresInHours"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("fingerprintHash", fingerprintHash),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string OtpCacheKey(Guid userId, string fingerprintHash) =>
        $"device-otp:{userId}:{fingerprintHash}";

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string userAgent, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("帳號或密碼錯誤");

        var fingerprintHash = HashFingerprint(request.Fingerprint);

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == user.Id && d.FingerprintHash == fingerprintHash, ct);

        if (device is { IsVerified: true })
        {
            device.LastSeenAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return new LoginResponse(GenerateJwt(user, fingerprintHash), false, null, null);
        }

        var otp = Random.Shared.Next(100000, 999999).ToString();
        var cacheKey = OtpCacheKey(user.Id, fingerprintHash);

        await cache.SetAsync(cacheKey, otp,
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            cancellationToken: ct);

        logger.LogInformation("[OTP] 使用者 {Username} 新裝置驗證碼：{Otp}", user.Username, otp);

        return new LoginResponse(null, true, user.Id.ToString(), fingerprintHash);
    }

    public async Task<VerifyDeviceResponse> VerifyDeviceAsync(VerifyDeviceRequest request, CancellationToken ct = default)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new ArgumentException("無效的 UserId");

        var cacheKey = OtpCacheKey(userId, request.FingerprintHash);
        var storedOtp = await cache.GetOrCreateAsync<string?>(cacheKey, _ => ValueTask.FromResult<string?>(null), cancellationToken: ct);

        if (storedOtp is null || storedOtp != request.Otp)
            throw new UnauthorizedAccessException("OTP 無效或已過期");

        await cache.RemoveAsync(cacheKey, ct);

        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UnauthorizedAccessException("使用者不存在");

        var existing = await db.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.FingerprintHash == request.FingerprintHash, ct);

        if (existing is null)
        {
            db.UserDevices.Add(new UserDevice
            {
                UserId = userId,
                FingerprintHash = request.FingerprintHash,
                DeviceName = request.DeviceName ?? "Unknown Device",
                UserAgent = request.UserAgent ?? string.Empty,
                IsVerified = true,
            });
        }
        else
        {
            existing.IsVerified = true;
            existing.LastSeenAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return new VerifyDeviceResponse(GenerateJwt(user, request.FingerprintHash));
    }
}
