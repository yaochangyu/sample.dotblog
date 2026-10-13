using StackExchange.Redis;

namespace Lab.Signature.WebApi.Services;

/// <summary>
/// 以 Redis 為基礎的 Nonce 防重放實作。
/// 透過 Redis 的原子操作 `SET {key} 1 NX EX {ttl}` 防止併發與重放攻擊。
/// </summary>
public class RedisNonceStore : INonceStore
{
    private static readonly TimeSpan NonceExpiration = TimeSpan.FromMinutes(5);
    private readonly IConnectionMultiplexer _redis;

    public RedisNonceStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> TryConsumeAsync(string apiKey, string nonce, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = BuildCacheKey(apiKey, nonce);

        // 使用 Redis NX (Not Exists) 原子操作寫入，過期時間為 5 分鐘
        var acquired = await db.StringSetAsync(
            key,
            "1",
            expiry: NonceExpiration,
            when: When.NotExists);

        return acquired;
    }

    private static string BuildCacheKey(string apiKey, string nonce) => $"api_nonce:{apiKey}:{nonce.ToLowerInvariant()}";
}
