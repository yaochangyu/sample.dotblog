namespace Lab.Signature.WebApi.Services;

/// <summary>
/// Nonce 防重放服務。教學版用 <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> 實作，
/// 僅適合單機教學：多實例部署或服務重啟會讓防重放失效，正式環境應改用 Redis `SET NX EX` 或 DB unique constraint。
/// </summary>
public interface INonceStore
{
    /// <summary>
    /// 嘗試消費一個 Nonce：若該 ApiKey+Nonce 組合尚未使用過，標記為已用並回傳 true；
    /// 若已使用過，回傳 false。此操作對同一 Key 是原子的（不會被併發請求同時通過）。
    /// </summary>
    Task<bool> TryConsumeAsync(string apiKey, string nonce, CancellationToken cancellationToken = default);
}
