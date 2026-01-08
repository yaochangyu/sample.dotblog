using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace Lab.CSRF.WebApi.Services;

public class CsrfTokenService : ICsrfTokenService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CsrfTokenService> _logger;
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromMinutes(30);

    public CsrfTokenService(IMemoryCache cache, ILogger<CsrfTokenService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string GenerateToken()
    {
        var token = GenerateRandomToken();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _tokenExpiration
        };

        _cache.Set(token, true, cacheOptions);

        _logger.LogInformation("產生新的 CSRF Token: {Token}", token);

        return token;
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token 為空");
            return false;
        }

        var exists = _cache.TryGetValue(token, out _);

        if (!exists)
        {
            _logger.LogWarning("Token 無效或已過期: {Token}", token);
        }
        else
        {
            _logger.LogInformation("Token 驗證成功: {Token}", token);
        }

        return exists;
    }

    public void RemoveToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _cache.Remove(token);
        _logger.LogInformation("移除 Token: {Token}", token);
    }

    private static string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
