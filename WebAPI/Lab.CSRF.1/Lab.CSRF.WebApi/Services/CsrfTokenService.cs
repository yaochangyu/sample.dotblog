using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;

namespace Lab.CSRF.WebApi.Services;

public class CsrfTokenService : ICsrfTokenService
{
    private readonly IDistributedCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CsrfTokenService> _logger;
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromMinutes(30);

    public CsrfTokenService(
        IDistributedCache cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CsrfTokenService> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GenerateToken()
    {
        // 取得或建立 Session
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            _logger.LogWarning("無法取得 Session");
            throw new InvalidOperationException("Session 未啟用");
        }

        // 觸發 Session 的建立（寫入一個標記以確保 Session Cookie 被設定）
        httpContext.Session.SetString("_csrf_initialized", "true");

        // 取得 Session ID
        var sessionId = httpContext.Session.Id;

        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("無法取得 Session ID");
            throw new InvalidOperationException("Session 未啟用");
        }

        // 產生 Token
        var token = GenerateRandomToken();

        // 將 Token 與 Session ID 綁定
        var cacheKey = $"csrf:{sessionId}:{token}";
        _cache.SetString(cacheKey, "valid", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _tokenExpiration
        });

        _logger.LogInformation("產生新的 CSRF Token，綁定 Session: {SessionId}", sessionId);

        return token;
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token 為空");
            return false;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var sessionId = httpContext?.Session?.Id;

        if (string.IsNullOrEmpty(sessionId))
        {
            _logger.LogWarning("無法取得 Session ID 進行驗證");
            return false;
        }

        // 驗證 Token 是否與當前 Session 綁定
        var cacheKey = $"csrf:{sessionId}:{token}";
        var cachedValue = _cache.GetString(cacheKey);

        if (cachedValue == null)
        {
            _logger.LogWarning("Token 無效或已過期，Session: {SessionId}, Token: {Token}",
                sessionId, token);
            return false;
        }

        _logger.LogInformation("Token 驗證成功，Session: {SessionId}", sessionId);
        return true;
    }

    public void RemoveToken(string token)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var sessionId = httpContext?.Session?.Id;

        if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrWhiteSpace(token))
        {
            var cacheKey = $"csrf:{sessionId}:{token}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("移除 Token，Session: {SessionId}", sessionId);
        }
    }

    private static string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
