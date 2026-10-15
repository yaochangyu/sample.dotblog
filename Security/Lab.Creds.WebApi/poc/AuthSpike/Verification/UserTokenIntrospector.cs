using System.Security.Claims;
using System.Text.Json;
using AuthSpike.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthSpike.Verification;

/// <summary>
/// 使用者 Token 的查證（02 單）：以資源端的使用者身分 <see cref="UserCallers.IntrospectionClientId"/> 與同一個 mTLS 憑證向授權伺服器內省。
/// 使用此身分才能通過授權伺服器對 audience 的比對（使用者 Token 的 audience 是使用者 audience）。
/// 內省結果在本類別轉為 principal：只保留 sub、client_id、caller_type、aud 與 scope；簽發者與有效期於此檢查。
/// 查證失敗（連線、非 200、解析錯誤）一律回報 Unavailable，不當成 Token 無效（fail closed）。
/// </summary>
public sealed class UserTokenIntrospector : IDisposable
{
    private readonly Uri _issuer;
    private readonly Uri _endpoint;
    private readonly HttpClient _http;

    public UserTokenIntrospector(Uri issuer, System.Security.Cryptography.X509Certificates.X509Certificate2 clientCertificate, SpikeTrust trust)
    {
        _issuer = issuer;
        _endpoint = new Uri(issuer, "connect/introspect");
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = trust.ServerCertificateValidator,
        };
        handler.ClientCertificates.Add(clientCertificate);
        _http = new HttpClient(handler);
    }

    public async Task<(CallerOutcome Outcome, ClaimsPrincipal? Principal, DateTimeOffset? ExpiresAt)> IntrospectAsync(string token)
    {
        try
        {
            using var response = await _http.PostAsync(_endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = "access_token",
                ["client_id"] = UserCallers.IntrospectionClientId,
            }));
            if (!response.IsSuccessStatusCode)
            {
                return (CallerOutcome.Unavailable, null, null);
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            if (!root.TryGetProperty("active", out var active) || active.ValueKind != JsonValueKind.True)
            {
                return (CallerOutcome.Rejected, null, null);
            }

            // 簽發者必須是本授權伺服器；有效期已過者拒絕。
            if (root.TryGetProperty("iss", out var iss) && iss.GetString() != _issuer.AbsoluteUri)
            {
                return (CallerOutcome.Rejected, null, null);
            }

            var expiresAt = root.TryGetProperty("exp", out var exp) ? DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) : (DateTimeOffset?)null;
            if (expiresAt is { } expiry && expiry <= DateTimeOffset.UtcNow)
            {
                return (CallerOutcome.Rejected, null, null);
            }

            var identity = new ClaimsIdentity("introspection", Claims.Name, Claims.Role);
            identity.AddClaim(new Claim(Claims.Subject, Required(root, Claims.Subject)));
            identity.AddClaim(new Claim(Claims.ClientId, Required(root, Claims.ClientId)));
            identity.AddClaim(new Claim(UserCallers.CallerTypeClaim, Required(root, UserCallers.CallerTypeClaim)));
            foreach (var audience in Strings(root, Claims.Audience))
            {
                identity.AddClaim(new Claim(Claims.Audience, audience));
            }

            foreach (var scope in (Required(root, Claims.Scope)).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                identity.AddClaim(new Claim(Claims.Private.Scope, scope));
            }

            return (CallerOutcome.Verified, new ClaimsPrincipal(identity), expiresAt);
        }
        catch (Exception)
        {
            // 連線、逾時、回應格式錯誤等無法確認 Token 是否有效：fail closed 為 503。
            return (CallerOutcome.Unavailable, null, null);
        }
    }

    private static string Required(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new InvalidOperationException($"Introspection response is missing '{name}'.");

    /// <summary>aud 可能是字串或陣列。</summary>
    private static IEnumerable<string> Strings(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return [];
        }

        return value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetString()!).ToList()
            : [value.GetString()!];
    }

    public void Dispose() => _http.Dispose();
}
