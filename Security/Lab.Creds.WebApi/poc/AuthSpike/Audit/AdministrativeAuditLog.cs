namespace AuthSpike.Audit;

/// <summary>管理操作稽核紀錄無法寫入；管理操作必須不生效，並以 503 audit_unavailable 明確回報，不得假報成功。</summary>
public sealed class AdministrativeAuditWriteFailedException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// 管理操作稽核紀錄（15 單）：記錄信任名單的每一次改變（登錄申請、核准、拒絕、退役、撤銷、停用）。
/// 只含操作者、時間、對象、指紋與結果：不含私鑰、原始 Token、Authorization 標頭或憑證私有內容。
/// ActorStatus 為「已驗證管理員」時才是管理員操作；其餘（含被拒絕的呼叫）一律為「未驗證」，ActorThumbprint 只是出示的憑證指紋，不視為身分。
/// Fingerprint：憑證為 SHA-1 Thumbprint，簽章金鑰為公開金鑰 SubjectPublicKeyInfo 的 SHA-256。
/// </summary>
public sealed record AdministrativeAuditRecord(
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string Operation,
    string ActorStatus,
    string? ActorThumbprint,
    string? ClientId,
    string? Subject,
    string? KeyId,
    string? Fingerprint,
    string Outcome,
    string Reason,
    int ResultStatus);

/// <summary>
/// 管理操作稽核（與呼叫者安全稽核 <see cref="SecurityAuditLog"/> 分開管理；lab 以記憶體保存）。
/// 保存期（lab 暫定，待使用者確認）：90 天，寫入時清除過期紀錄。
/// 存取規則（lab 暫定，待使用者確認）：僅追加（append-only），不提供修改或刪除；只經管理員身分的查詢端點讀取，不對業務 API 公開。
/// </summary>
public sealed class AdministrativeAuditLog
{
    /// <summary>管理操作稽核保存期（lab 暫定，待使用者確認）。</summary>
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    private readonly object _gate = new();
    private readonly List<AdministrativeAuditRecord> _records = [];

    /// <summary>lab 故障模擬：為 true 時寫入一律失敗。</summary>
    public bool SimulateWriteFailure { get; set; }

    /// <summary>追加一筆紀錄；寫入失敗時拋出 <see cref="AdministrativeAuditWriteFailedException"/>，呼叫端不得套用管理操作。</summary>
    public void Append(AdministrativeAuditRecord record)
    {
        lock (_gate)
        {
            if (SimulateWriteFailure)
            {
                throw new AdministrativeAuditWriteFailedException("模擬管理操作稽核寫入失敗。");
            }

            var cutoff = DateTimeOffset.UtcNow - RetentionPeriod;
            _records.RemoveAll(existing => existing.OccurredAt < cutoff);
            _records.Add(record);
        }
    }

    /// <summary>回傳目前保存中的紀錄快照（唯讀）。</summary>
    public IReadOnlyList<AdministrativeAuditRecord> Snapshot()
    {
        lock (_gate)
        {
            return _records.ToList();
        }
    }
}
