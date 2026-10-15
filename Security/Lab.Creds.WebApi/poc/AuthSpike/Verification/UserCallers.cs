using System.Security.Claims;
using OpenIddict.Abstractions;

namespace AuthSpike.Verification;

/// <summary>
/// 使用者呼叫者的表示方式（02 單）：使用者 Token 由同一個授權伺服器簽發，audience 與呼叫者類型皆不同於服務 Token。
/// 所有數值為 lab 暫定值，待使用者確認；測試使用者帳號代替真實登入流程。
/// </summary>
public static class UserCallers
{
    /// <summary>取得使用者 Token 時使用的公開 Client（password grant，lab 暫定）。</summary>
    public const string ClientId = "orders-user-client";

    /// <summary>使用者 Token 的 audience；業務 API 只接受此 audience 走使用者路徑。</summary>
    public const string Audience = "orders-api-user";

    /// <summary>業務 API 的使用者路徑內省時使用的資源端身分（與使用者 audience 相同，授權伺服器據此比對 audience）。</summary>
    public const string IntrospectionClientId = Audience;

    /// <summary>Token 內標示呼叫者類型的 claim 名稱與使用者類型的值。</summary>
    public const string CallerTypeClaim = "caller_type";
    public const string CallerTypeValue = "user";

    /// <summary>測試使用者帳號（lab 代替真實登入）。</summary>
    public const string TestUserId = "test-user-1";
    public const string TestUserPassword = "lab-only-password";

    /// <summary>使用者可被核發的 scope（與服務的 scope 白名單分開）。</summary>
    public static readonly string[] Scopes = ["orders.read", "orders.write"];

    /// <summary>測試使用者帳密比對（lab 暫定）；只接受測試使用者帳號與其密碼。</summary>
    public static bool VerifyCredentials(string? username, string? password)
        => username == TestUserId && password == TestUserPassword;

    public static bool IsUserPrincipal(ClaimsPrincipal principal) => principal.GetClaim(CallerTypeClaim) == CallerTypeValue;
}
