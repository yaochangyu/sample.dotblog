namespace IdempotencyKey.WebApi.IdempotencyKeys;

public class IdempotencyKeyRecord
{
    /// <summary>由客戶端傳入的 Idempotency-Key header 值</summary>
    public string Key { get; set; } = string.Empty;

    public IdempotencyKeyStatus Status { get; set; } = IdempotencyKeyStatus.InProgress;

    /// <summary>Request body 的 SHA-256 fingerprint，防止相同 key 搭配不同 payload</summary>
    public string? RequestFingerprint { get; set; }

    /// <summary>首次執行的回應 HTTP status code（供重播使用）</summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>首次執行的回應 body（供重播使用）</summary>
    public string? ResponseBody { get; set; }

    /// <summary>首次執行的回應 Content-Type（供重播使用）</summary>
    public string? ResponseContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Key 過期時間，到期後允許相同 key 的新請求</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
