namespace IdempotencyKey.WebApi.IdempotencyKeys;

public interface IIdempotencyKeyStore
{
    /// <summary>
    /// 嘗試以原子操作建立 IN_PROGRESS 狀態的 key。
    /// 回傳 null 表示成功取得鎖；回傳現有記錄表示 key 已存在。
    /// </summary>
    /// <param name="ttlHours">key 完成後的保留時間（小時）。</param>
    /// <param name="lockTtlSeconds">InProgress 狀態的鎖定時間（秒），系統崩潰後自動釋放。預設 30 秒。</param>
    Task<IdempotencyKeyRecord?> TryAcquireAsync(string key, string fingerprint, int ttlHours,
        int lockTtlSeconds = 30, CancellationToken ct = default);

    Task SetCompletedAsync(string key, int statusCode, string? body, string? contentType,
        int ttlHours, CancellationToken ct = default);

    Task SetFailedAsync(string key, int statusCode, string? body, string? contentType,
        int ttlHours, CancellationToken ct = default);

    /// <summary>刪除 key，讓客戶端可用相同 key 重試（暫時性失敗時使用）</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}
