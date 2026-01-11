using Microsoft.Extensions.Caching.Memory;

namespace Lab.CSRF2.WebAPI.Services;

public class TokenService : ITokenService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IMemoryCache cache, ILogger<TokenService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string GenerateToken(int maxUsageCount = 1, int expirationMinutes = 5)
    {
        var token = Guid.NewGuid().ToString();
        var tokenData = new TokenData
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            MaxUsageCount = maxUsageCount,
            UsageCount = 0
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenData.ExpiresAt
        };

        _cache.Set(token, tokenData, cacheOptions);
        _logger.LogInformation("Token generated: {Token}, Expires: {ExpiresAt}, MaxUsage: {MaxUsage}", 
            token, tokenData.ExpiresAt, maxUsageCount);

        return token;
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token validation failed: Token is null or empty");
            return false;
        }

        if (!_cache.TryGetValue(token, out TokenData? tokenData) || tokenData == null)
        {
            _logger.LogWarning("Token validation failed: Token not found - {Token}", token);
            return false;
        }

        if (DateTime.UtcNow > tokenData.ExpiresAt)
        {
            _logger.LogWarning("Token validation failed: Token expired - {Token}", token);
            _cache.Remove(token);
            return false;
        }

        if (tokenData.UsageCount >= tokenData.MaxUsageCount)
        {
            _logger.LogWarning("Token validation failed: Usage count exceeded - {Token}", token);
            _cache.Remove(token);
            return false;
        }

        tokenData.UsageCount++;
        _cache.Set(token, tokenData, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenData.ExpiresAt
        });

        _logger.LogInformation("Token validated successfully: {Token}, Usage: {Usage}/{MaxUsage}", 
            token, tokenData.UsageCount, tokenData.MaxUsageCount);

        if (tokenData.UsageCount >= tokenData.MaxUsageCount)
        {
            _cache.Remove(token);
            _logger.LogInformation("Token removed after reaching max usage: {Token}", token);
        }

        return true;
    }
}

public class TokenData
{
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MaxUsageCount { get; set; }
    public int UsageCount { get; set; }
}
