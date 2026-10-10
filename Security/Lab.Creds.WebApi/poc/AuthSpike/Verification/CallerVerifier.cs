using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthSpike.Trust;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthSpike.Verification;

/// <summary>查證結果：Verified 才能進入業務處理；Unavailable 表示查證服務無法連線且沒有有效快取，必須明確回報服務錯誤。</summary>
public enum CallerOutcome
{
    Verified,
    Rejected,
    Unavailable,
}

/// <summary>
/// 業務 API 的呼叫者查證閘門。
/// Token 由授權伺服器 introspection 查證（OpenIddict validation，含 mTLS 憑證綁定）；成功結果快取至多 lifetime（lab 暫定）。
/// 快取到期即重新查證；查證失敗不更新快取，因此失敗不會延長快取有效期限。
/// Client 啟用狀態與 mTLS 憑證撤銷狀態每次請求都直接讀取 TrustRegistry，不經快取。
/// </summary>
public sealed class CallerVerifier(TrustRegistry registry, TimeSpan lifetime)
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>最近一次請求的來源埠（用於確認既有 TCP 連線是否被重用）。</summary>
    public int LastRemotePort { get; private set; }

    public async Task<(CallerOutcome Outcome, ClaimsPrincipal? Principal)> VerifyAsync(HttpContext context)
    {
        LastRemotePort = context.Connection.RemotePort;

        var token = BearerToken(context.Request.Headers.Authorization.ToString());
        var certificate = context.Connection.ClientCertificate;
        if (token is null || certificate is null)
        {
            return (CallerOutcome.Rejected, null);
        }

        var thumbprint = certificate.Thumbprint;
        var key = CacheKey(token, thumbprint);
        var now = DateTimeOffset.UtcNow;

        ClaimsPrincipal principal;
        if (_cache.TryGetValue(key, out var entry) && now < entry.ExpiresAt)
        {
            principal = entry.Principal;
        }
        else
        {
            _cache.TryRemove(key, out _);

            AuthenticateResult result;
            try
            {
                result = await context.AuthenticateAsync();
            }
            catch (Exception exception) when (IsUnavailable(exception))
            {
                return (CallerOutcome.Unavailable, null);
            }

            if (!result.Succeeded)
            {
                // 授權伺服器無法連線時，OpenIddict 以 server_error 表示查證服務不可用，而非 Token 無效。
                return IsServerError(result) ? (CallerOutcome.Unavailable, null) : (CallerOutcome.Rejected, null);
            }

            principal = result.Principal!;
            var expiresAt = now + lifetime;
            if (result.Properties?.ExpiresUtc is { } tokenExpiresAt && tokenExpiresAt < expiresAt)
            {
                expiresAt = tokenExpiresAt;
            }

            _cache[key] = new CacheEntry(principal, expiresAt);
        }

        var clientId = principal.GetClaim(Claims.ClientId);
        if (clientId is null || !registry.IsClientEnabled(clientId) || registry.IsCertificateRevoked(thumbprint))
        {
            // 查證已通過但 Client 或憑證已停用：principal 僅供稽核標示為未驗證的宣稱身分，業務處理不採信。
            return (CallerOutcome.Rejected, principal);
        }

        return (CallerOutcome.Verified, principal);
    }

    private sealed record CacheEntry(ClaimsPrincipal Principal, DateTimeOffset ExpiresAt);

    private static string? BearerToken(string header)
        => header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;

    private static string CacheKey(string token, string thumbprint)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{token}|{thumbprint}")));

    /// <summary>OpenIddict 在查證服務無法取得回應時設定的錯誤代碼（屬性鍵 .error）。</summary>
    private static bool IsServerError(AuthenticateResult result)
        => result.Properties?.Items.TryGetValue(".error", out var error) == true && error == Errors.ServerError;

    /// <summary>查證服務無法連線（網路層錯誤或逾時）時視為暫時無法查證，而非 Token 無效。</summary>
    private static bool IsUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException or TimeoutException or TaskCanceledException)
            {
                return true;
            }
        }

        return false;
    }
}
