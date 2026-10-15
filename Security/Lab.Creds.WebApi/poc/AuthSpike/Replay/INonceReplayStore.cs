namespace AuthSpike.Replay;

/// <summary>防重放登錄結果。</summary>
public enum ReplayRegistration
{
    /// <summary>首次登錄成功。</summary>
    Registered,

    /// <summary>已存在（重放）。</summary>
    Replayed,

    /// <summary>簽章建立時間早於防重放紀元（Redis 重啟或被清空後重建），一律拒絕。</summary>
    BeforeReplayEpoch,
}

/// <summary>
/// 防重放儲存介面：跨執行個體登錄與檢查簽章 Nonce，並支援防重放紀元（Replay Epoch）。
/// </summary>
public interface INonceReplayStore
{
    /// <summary>
    /// 嘗試登錄 (clientId, nonce)。
    /// </summary>
    /// <param name="clientId">已驗證的 Client ID</param>
    /// <param name="nonce">簽章中的 Nonce</param>
    /// <param name="created">簽章建立時間（用於防重放紀元檢核）</param>
    /// <param name="retainUntil">保存期限（通常為 expires + clock skew）</param>
    Task<ReplayRegistration> TryRegisterAsync(string clientId, string nonce, DateTimeOffset created, DateTimeOffset retainUntil);

    /// <summary>查詢紀錄保存期限（測試用；若不存在則回傳 null）。</summary>
    Task<DateTimeOffset?> RetainUntilOfAsync(string clientId, string nonce);

    /// <summary>查詢目前防重放紀元時間（若不存在則回傳 null）。</summary>
    Task<DateTimeOffset?> GetEpochAsync();
}
