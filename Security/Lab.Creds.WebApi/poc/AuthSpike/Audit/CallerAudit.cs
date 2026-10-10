using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace AuthSpike.Audit;

/// <summary>
/// 業務請求的稽核紀錄寫入點。每筆請求最多由一個決策點寫入一次（以 Items 標記 Decided）。
/// 只有簽章驗證通過才會填入 verifiedClientId；其他情況宣稱的身分一律放在未驗證欄位。
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
        string? verifiedClientId = null,
        string? signatureKeyId = null,
        string? unverifiedTokenClientId = null)
    {
        context.Items[DecidedKey] = true;

        var record = new SecurityAuditRecord(
            CorrelationId: context.TraceIdentifier,
            OccurredAt: DateTimeOffset.UtcNow,
            Operation: $"{context.Request.Method} {context.Request.Path}",
            Outcome: outcome,
            Reason: reason,
            VerifiedClientId: verifiedClientId,
            SignatureKeyId: signatureKeyId,
            UnverifiedTokenClientId: unverifiedTokenClientId,
            PresentedClientIdHeader: PresentedClientId(context),
            CertificateThumbprint: context.Connection.ClientCertificate?.Thumbprint);

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
