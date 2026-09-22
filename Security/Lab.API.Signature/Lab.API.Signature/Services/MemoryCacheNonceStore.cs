using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Lab.API.Signature.Services;

/// <summary>
/// 以 <see cref="IMemoryCache"/> 為基礎的 Nonce 防重放實作。
/// Cache Key 為 "{ApiKey}:{Nonce}"，避免不同 client 互相碰撞；過期時間比照 Timestamp 視窗（5 分鐘）。
/// 用 per-key <see cref="SemaphoreSlim"/> 確保「檢查存在→標記已用」是原子操作，避免併發 replay 同時通過同一 Nonce。
/// </summary>
public class MemoryCacheNonceStore : INonceStore
{
    private static readonly TimeSpan NonceExpiration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public MemoryCacheNonceStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> TryConsumeAsync(string apiKey, string nonce, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(apiKey, nonce);
        var keyLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out _))
            {
                return false;
            }

            _cache.Set(cacheKey, true, NonceExpiration);
            return true;
        }
        finally
        {
            keyLock.Release();
        }

        // 教學簡化：刻意不從 _locks 移除已用完的 SemaphoreSlim。
        // 若在 Release() 之後立即移除，會與另一個仍持有同一實例參照、尚未完成等待的併發呼叫產生
        // race condition（第三個呼叫可能因為字典已移除該 key 而建立出「第二個」semaphore，
        // 導致同一組 ApiKey+Nonce 同時有兩個鎖在跑，破壞原子性保證）。
        // 因此保留全部 per-key lock 物件，記憶體會隨曾出現過的相異 Nonce 數量緩慢成長，
        // 正式環境改用 Redis SET NX EX 時不會有這個問題。
    }

    private static string BuildCacheKey(string apiKey, string nonce) => $"{apiKey}:{nonce}";
}
