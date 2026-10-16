using System.Security.Claims;
using System.Text.RegularExpressions;
using AuthSpike.Verification;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthSpike.Audit;

/// <summary>
/// 宣稱的呼叫者（未驗證）：驗證失敗或授權前即被拒絕的請求，其宣稱的呼叫者類型與身分只能寫入未驗證欄位（07 單）。
/// 型別與身分取自已通過或未通過驗證的 Token claims，只用於稽核標示，不得進入授權或業務處理。
/// </summary>
public sealed record ClaimedCaller(CallerType Type, string? Identity)
{
    /// <summary>由 Token 的 claims 取得宣稱的呼叫者；帶有 caller_type=user 的 Token 為使用者，其餘為服務。</summary>
    public static ClaimedCaller? FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        if (UserCallers.IsUserPrincipal(principal))
        {
            return new(CallerType.User, principal.GetClaim(Claims.Subject));
        }

        var clientId = principal.GetClaim(Claims.ClientId);
        return clientId is null ? null : new(CallerType.Service, clientId);
    }

    /// <summary>已通過 Token 查證的呼叫者，在後續驗證（簽章、防重放）失敗時仍只能視為宣稱身分。</summary>
    public static ClaimedCaller FromVerified(VerifiedCaller caller) => new(caller.Type, caller.Identity);
}

/// <summary>
/// 業務請求的稽核紀錄寫入點。每筆請求最多由一個決策點寫入一次（以 Items 標記 Decided）。
/// 只有呼叫者類型對應的驗證成功才會填入已驗證呼叫者（類型與身分）；其他情況宣稱的呼叫者一律放在未驗證欄位。
/// </summary>
public static class CallerAudit
{
    private const string DecidedKey = "AuthSpike.Audit.Decided";
    private const int MaxPresentedLength = 64;
    private static readonly Regex UnsafeCharacters = new("[^A-Za-z0-9._-]", RegexOptions.Compiled);

    /// <summary>是否已有決策點處理過本請求（不論紀錄是否寫入成功）。</summary>
    public static bool IsDecided(HttpContext context) => context.Items.ContainsKey(DecidedKey);

    /// <returns>true 表示紀錄已寫入；false 表示寫入失敗，呼叫端必須以 audit_unavailable 明確回報，不得放行。</returns>
    public static bool TryRecord(
        HttpContext context,
        SecurityAuditLog audit,
        string outcome,
        string reason,
        VerifiedCaller? verifiedCaller = null,
        string? signatureKeyId = null,
        ClaimedCaller? claimedCaller = null)
    {
        context.Items[DecidedKey] = true;

        var record = new SecurityAuditRecord(
            CorrelationId: context.TraceIdentifier,
            OccurredAt: DateTimeOffset.UtcNow,
            Operation: $"{context.Request.Method} {context.Request.Path}",
            Outcome: outcome,
            Reason: reason,
            VerifiedClientId: verifiedCaller?.Type == CallerType.Service ? verifiedCaller.Identity : null,
            SignatureKeyId: signatureKeyId,
            UnverifiedTokenClientId: claimedCaller?.Type == CallerType.Service ? claimedCaller.Identity : null,
            PresentedClientIdHeader: PresentedClientId(context),
            CertificateThumbprint: context.Connection.ClientCertificate?.Thumbprint,
            VerifiedCallerType: verifiedCaller is null ? null : CallerTypeName(verifiedCaller.Type),
            VerifiedCallerIdentity: verifiedCaller?.Identity,
            UnverifiedTokenCallerType: claimedCaller is null ? null : CallerTypeName(claimedCaller.Type),
            UnverifiedTokenIdentity: claimedCaller?.Identity);

        try
        {
            audit.Append(record);
            return true;
        }
        catch (SecurityAuditWriteFailedException)
        {
            return false;
        }
    }

    /// <summary>呼叫者類型的稽核標示（lab 暫定、待使用者確認）：service 或 user。</summary>
    private static string CallerTypeName(CallerType type) => type == CallerType.User ? "user" : "service";

    /// <summary>X-Client-Id 只是宣稱值：去除非常規字元並截斷，避免紀錄被注入控制字元或超長內容。</summary>
    private static string? PresentedClientId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-Client-Id", out var values))
        {
            return null;
        }

        var sanitized = UnsafeCharacters.Replace(values.ToString(), "?");
        return sanitized.Length > MaxPresentedLength ? sanitized[..MaxPresentedLength] : sanitized;
    }
}
