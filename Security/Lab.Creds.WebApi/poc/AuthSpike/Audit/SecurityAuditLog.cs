namespace AuthSpike.Audit;

/// <summary>稽核紀錄無法寫入；呼叫端必須以 503 明確回報（audit_unavailable），不得放行業務處理，也不得假報成功。</summary>
public sealed class SecurityAuditWriteFailedException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// 單筆業務請求的安全稽核紀錄。
/// 只含識別與結果欄位：不含 Token、Authorization／Signature／Signature-Input 標頭值、簽章基底、私鑰或 Body。
/// VerifiedClientId 僅在簽章驗證通過時有值；其他情況宣稱的 Client 一律放在 Unverified／Presented 欄位並視為未驗證。
/// </summary>
public sealed record SecurityAuditRecord(
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string Operation,
    string Outcome,
    string Reason,
    string? VerifiedClientId,
    string? SignatureKeyId,
    string? UnverifiedTokenClientId,
    string? PresentedClientIdHeader,
    string? CertificateThumbprint);

/// <summary>
/// 安全稽核紀錄（與防重放 nonce 及業務訂單儲存分開；lab 以記憶體保存）。
/// 保存期（lab 暫定，待使用者確認）：90 天，寫入時清除過期紀錄。
/// 存取規則（lab 暫定，待使用者確認）：僅追加（append-only），不提供修改或刪除；不經業務 API 公開，僅維護者程序內讀取。
/// </summary>
public sealed class SecurityAuditLog
{
    /// <summary>稽核紀錄保存期（lab 暫定）。</summary>
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    private readonly object _gate = new();
    private readonly List<SecurityAuditRecord> _records = [];

    /// <summary>lab 故障模擬：為 true 時寫入一律失敗。</summary>
    public bool SimulateWriteFailure { get; set; }

    /// <summary>追加一筆紀錄；寫入失敗時拋出 <see cref="SecurityAuditWriteFailedException"/>，不得靜默吞掉。</summary>
    public void Append(SecurityAuditRecord record)
    {
        lock (_gate)
        {
            if (SimulateWriteFailure)
            {
                throw new SecurityAuditWriteFailedException("模擬稽核紀錄寫入失敗。");
            }

            var cutoff = DateTimeOffset.UtcNow - RetentionPeriod;
            _records.RemoveAll(existing => existing.OccurredAt < cutoff);
            _records.Add(record);
        }
    }

    /// <summary>回傳目前保存中的紀錄快照（唯讀）。</summary>
    public IReadOnlyList<SecurityAuditRecord> Snapshot()
    {
        lock (_gate)
        {
            return _records.ToList();
        }
    }
}
