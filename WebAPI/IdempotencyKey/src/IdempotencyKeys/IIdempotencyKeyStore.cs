namespace IdempotencyKey.WebApi.IdempotencyKeys;

public interface IIdempotencyKeyStore
{
    /// <summary>
    /// 嘗試以原子操作建立 IN_PROGRESS 狀態的 key。
    /// 回傳 null 表示成功取得鎖；回傳現有記錄表示 key 已存在。
    /// </summary>
    Task<IdempotencyKeyRecord?> TryAcquireAsync(string key, string fingerprint, int ttlHours,
        CancellationToken ct = default);

    Task SetCompletedAsync(string key, int statusCode, string? body, string? contentType,
        CancellationToken ct = default);

    Task SetFailedAsync(string key, int statusCode, string? body, string? contentType,
        CancellationToken ct = default);

    /// <summary>刪除 key，讓客戶端可用相同 key 重試（暫時性失敗時使用）</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}
