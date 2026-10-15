using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthSpike.Verification;

/// <summary>呼叫者類型：由通過的驗證路徑決定，不由請求內容宣稱。目前只有服務（使用者呼叫者於後續票加入）。</summary>
public enum CallerType
{
    Service,
}

/// <summary>
/// 已驗證呼叫者：呼叫者類型、已驗證身分（Client）與權限（scope）。
/// 只在呼叫者類型對應的驗證成功後才會附加到請求上；驗證失敗時不附加，宣稱的身分只能標為未驗證。
/// 授權規則與業務處理只依此判斷，不直接依驗證的內部細節（例如 Token 的 claim 或簽章金鑰）。
/// </summary>
public sealed record VerifiedCaller(CallerType Type, string Identity, IReadOnlyList<string> Permissions)
{
    private const string HttpContextItemKey = "AuthSpike.VerifiedCaller";

    /// <summary>由服務 Token 的 principal 建立已驗證的服務呼叫者；呼叫端需已確認 client_id 與 Token 有效。</summary>
    public static VerifiedCaller FromServicePrincipal(ClaimsPrincipal principal)
        => new(CallerType.Service, principal.GetClaim(Claims.ClientId) ?? throw new InvalidOperationException("Verified client_id is missing."), principal.GetScopes());

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public static void Attach(HttpContext context, VerifiedCaller caller) => context.Items[HttpContextItemKey] = caller;

    public static void Detach(HttpContext context) => context.Items.Remove(HttpContextItemKey);

    public static VerifiedCaller? TryGet(HttpContext context)
        => context.Items.TryGetValue(HttpContextItemKey, out var value) ? value as VerifiedCaller : null;

    /// <summary>業務處理端點使用；只會在查證閘門與簽章驗證都通過後執行，缺少時為程式錯誤。</summary>
    public static VerifiedCaller Get(HttpContext context)
        => TryGet(context) ?? throw new InvalidOperationException("Verified caller is missing.");
}
