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
/// 查證失敗（連線、逾時、非 200、非 JSON）一律回報 Unavailable，不當成 Token 無效（fail closed）。
/// 回應已是 JSON 但內容不符合使用者 Token（缺必要欄位、型別錯誤）是授權伺服器的明確判斷，回報 Rejected。
/// </summary>
public sealed class UserTokenIntrospector : IDisposable
{
    /// <summary>內省的逾時上限（lab 暫定、待使用者確認）：超過即視為查證服務無回應，fail closed 為 503。</summary>
    public static readonly TimeSpan IntrospectionTimeout = TimeSpan.FromSeconds(10);

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
        _http = new HttpClient(handler) { Timeout = IntrospectionTimeout };
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
            return Interpret(document.RootElement);
        }
        catch (Exception)
        {
            // 連線、逾時或非 JSON 等無法確認授權伺服器判斷的情況：fail closed 為 503。
            return (CallerOutcome.Unavailable, null, null);
        }
    }

    /// <summary>已解析的內省結果：內容不符合使用者 Token 的必要欄位或型別時，授權伺服器已明確回應，視為 Token 無效。</summary>
    private (CallerOutcome Outcome, ClaimsPrincipal? Principal, DateTimeOffset? ExpiresAt) Interpret(JsonElement root)
    {
        try
        {
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
        catch (Exception e) when (e is InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        {
            // 欄位缺漏或型別錯誤：授權伺服器的回應不是有效的使用者 Token，不是查證服務故障。
            return (CallerOutcome.Rejected, null, null);
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
