using Microsoft.Extensions.Caching.Memory;

namespace Lab.CSRF2.WebAPI.Providers;

public class TokenProvider : ITokenProvider
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenProvider> _logger;

    public TokenProvider(IMemoryCache cache, ILogger<TokenProvider> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string GenerateToken(int maxUsageCount, int expirationMinutes, string userAgent, string ipAddress)
    {
        var token = Guid.NewGuid().ToString();
        var tokenData = new TokenData
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            MaxUsageCount = maxUsageCount,
            UsageCount = 0,
            UserAgent = userAgent,
            IpAddress = ipAddress
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = tokenData.ExpiresAt
        };

        _cache.Set(token, tokenData, cacheOptions);
        _logger.LogInformation("Token generated: {Token}, Expires: {ExpiresAt}, MaxUsage: {MaxUsage}, UA: {UA}, IP: {IP}", 
            token, tokenData.ExpiresAt, maxUsageCount, userAgent, ipAddress);

        return token;
    }

    public bool ValidateToken(string token, string userAgent, string ipAddress)
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

        // User-Agent 一致性檢查
        if (!string.IsNullOrEmpty(tokenData.UserAgent) && 
            !tokenData.UserAgent.Equals(userAgent, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Token validation failed: User-Agent mismatch. Expected: {Expected}, Got: {Actual}", 
                tokenData.UserAgent, userAgent);
            return false;
        }

        // IP 地址檢查 (可選 - 目前註解，避免開發環境問題)
        // if (!string.IsNullOrEmpty(tokenData.IpAddress) && tokenData.IpAddress != ipAddress)
        // {
        //     _logger.LogWarning("Token validation failed: IP mismatch. Expected: {Expected}, Got: {Actual}",
        //         tokenData.IpAddress, ipAddress);
        //     return false;
        // }

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
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
