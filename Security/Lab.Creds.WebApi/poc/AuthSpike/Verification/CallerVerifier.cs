using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthSpike.Trust;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
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
/// 業務 API 的呼叫者查證閘門，依驗證路徑決定呼叫者類型（02 單）。
/// 服務路徑：出示 mTLS 用戶端憑證時，服務 Token 由 OpenIddict validation 內省（audience 為本服務，含 mTLS cnf 綁定）。
/// 使用者路徑：未出示用戶端憑證時，使用者 Token 由 <see cref="UserTokenIntrospector"/> 內省，並要求使用者 audience、呼叫者類型與使用者有效狀態；不要求 mTLS。
/// 兩條路徑共用查證快取（至多 lifetime，lab 暫定）；快取到期即重新查證，查證失敗不更新快取。
/// Client 與使用者的有效狀態每次請求都直接讀取 TrustRegistry，不經快取。
/// </summary>
public sealed class CallerVerifier(TrustRegistry registry, TimeSpan lifetime, UserTokenIntrospector userIntrospector)
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>最近一次請求的來源埠（用於確認既有 TCP 連線是否被重用）。</summary>
    public int LastRemotePort { get; private set; }

    public async Task<(CallerOutcome Outcome, ClaimsPrincipal? Principal)> VerifyAsync(HttpContext context)
    {
        LastRemotePort = context.Connection.RemotePort;

        var token = BearerToken(context.Request.Headers.Authorization.ToString());
        if (token is null)
        {
            return (CallerOutcome.Rejected, null);
        }

        var certificate = context.Connection.ClientCertificate;
        return certificate is null
            ? await VerifyUserAsync(token)
            : await VerifyServiceAsync(context, token, certificate);
    }

    private async Task<(CallerOutcome Outcome, ClaimsPrincipal? Principal)> VerifyServiceAsync(HttpContext context, string token, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        var thumbprint = certificate.Thumbprint;
        if (registry.IsAdministratorCertificate(thumbprint))
        {
            // 管理員憑證不是業務呼叫者（11 單）：不得用於業務 API，也不進入快取或 introspection。
            return (CallerOutcome.Rejected, null);
        }

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
                result = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            }
            catch (Exception)
            {
                // 查證過程拋出任何例外（連線、逾時、回應格式錯誤等）都無法確認 Token 有效或無效，一律 fail closed 為 503。
                return (CallerOutcome.Unavailable, null);
            }

            if (!result.Succeeded)
            {
                // 授權伺服器無法連線或回應異常（5xx、非 JSON、空內容）時，OpenIddict 以 server_error 表示查證服務不可用，而非 Token 無效。
                return IsServerError(result) ? (CallerOutcome.Unavailable, null) : (CallerOutcome.Rejected, null);
            }

            principal = result.Principal!;
            Remember(key, principal, now, result.Properties?.ExpiresUtc);
        }

        var clientId = principal.GetClaim(Claims.ClientId);
        if (clientId is null || !registry.IsClientEnabled(clientId) || registry.IsCertificateBlocked(thumbprint))
        {
            // 查證已通過但 Client 或憑證已停用：principal 僅供稽核標示為未驗證的宣稱身分，業務處理不採信。
            return (CallerOutcome.Rejected, principal);
        }

        return (CallerOutcome.Verified, principal);
    }

    private async Task<(CallerOutcome Outcome, ClaimsPrincipal? Principal)> VerifyUserAsync(string token)
    {
        var key = CacheKey(token, string.Empty);
        var now = DateTimeOffset.UtcNow;

        ClaimsPrincipal principal;
        if (_cache.TryGetValue(key, out var entry) && now < entry.ExpiresAt)
        {
            principal = entry.Principal;
        }
        else
        {
            _cache.TryRemove(key, out _);

            var (outcome, introspected, expiresAt) = await userIntrospector.IntrospectAsync(token);
            if (outcome != CallerOutcome.Verified)
            {
                return (outcome, null);
            }

            principal = introspected!;
            Remember(key, principal, now, expiresAt);
        }

        // 使用者路徑：必須是使用者類型、audience 為使用者 audience，且使用者有效狀態為啟用（每次請求直接讀取）。
        var userId = principal.GetClaim(Claims.Subject);
        var accepted = UserCallers.IsUserPrincipal(principal)
            && principal.FindAll(Claims.Audience).Any(claim => claim.Value == UserCallers.Audience)
            && userId is not null
            && registry.IsUserEnabled(userId);
        return (accepted ? CallerOutcome.Verified : CallerOutcome.Rejected, principal);
    }

    private void Remember(string key, ClaimsPrincipal principal, DateTimeOffset now, DateTimeOffset? tokenExpiresAt)
    {
        var expiresAt = now + lifetime;
        if (tokenExpiresAt is { } tokenExpiry && tokenExpiry < expiresAt)
        {
            expiresAt = tokenExpiry;
        }

        _cache[key] = new CacheEntry(principal, expiresAt);
    }

    private sealed record CacheEntry(ClaimsPrincipal Principal, DateTimeOffset ExpiresAt);

    private static string? BearerToken(string header)
        => header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;

    private static string CacheKey(string token, string thumbprint)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{token}|{thumbprint}")));

    /// <summary>OpenIddict 在查證服務無法取得回應時設定的錯誤代碼（屬性鍵 .error）。</summary>
    private static bool IsServerError(AuthenticateResult result)
        => result.Properties?.Items.TryGetValue(".error", out var error) == true && error == Errors.ServerError;
}
