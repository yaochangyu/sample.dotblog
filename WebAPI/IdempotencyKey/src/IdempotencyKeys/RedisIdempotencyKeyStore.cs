using System.Text.Json;
using StackExchange.Redis;

namespace IdempotencyKey.WebApi.IdempotencyKeys;

public class RedisIdempotencyKeyStore(
    IConnectionMultiplexer redis,
    JsonSerializerOptions jsonOptions,
    ILogger<RedisIdempotencyKeyStore> logger)
    : IIdempotencyKeyStore
{
    private static string RedisKey(string key) => $"idempotency:{key}";

    public async Task<IdempotencyKeyRecord?> TryAcquireAsync(
        string key, string fingerprint, int ttlHours, int lockTtlSeconds = 30, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var redisKey = RedisKey(key);
        var lockTtl = TimeSpan.FromSeconds(lockTtlSeconds);
        var fullTtl = TimeSpan.FromHours(ttlHours);

        var record = new IdempotencyKeyRecord
        {
            Key = key,
            Status = IdempotencyKeyStatus.InProgress,
            RequestFingerprint = fingerprint,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(fullTtl)
        };

        var value = JsonSerializer.Serialize(record, jsonOptions);

        // SET NX EX：原子操作，只有 key 不存在時才寫入，使用短 TTL 作為鎖定期
        var acquired = await db.StringSetAsync(redisKey, value, lockTtl, When.NotExists);

        if (acquired)
        {
            logger.LogDebug("Redis SET NX succeeded for key {Key} (lock TTL: {LockTtl}s)", key, lockTtlSeconds);
            return null; // 成功取得鎖
        }

        // key 已存在，取得現有記錄
        var existing = await db.StringGetAsync(redisKey);
        if (existing.IsNullOrEmpty)
        {
            // 極端情況：SET NX 失敗但 GET 取不到（key 在這之間過期），視為可重試
            logger.LogWarning("Redis key {Key} disappeared between SET NX and GET, retrying acquire", key);
            acquired = await db.StringSetAsync(redisKey, value, lockTtl, When.NotExists);
            if (acquired)
                return null;

            existing = await db.StringGetAsync(redisKey);
        }

        if (existing.IsNullOrEmpty)
        {
            logger.LogWarning("Unable to read Redis key {Key} after multiple attempts", key);
            return new IdempotencyKeyRecord { Key = key, Status = IdempotencyKeyStatus.InProgress };
        }

        return JsonSerializer.Deserialize<IdempotencyKeyRecord>((string)existing!, jsonOptions);
    }

    public async Task SetCompletedAsync(
        string key, int statusCode, string? body, string? contentType, int ttlHours, CancellationToken ct = default)
    {
        await UpdateRecordAsync(key, statusCode, body, contentType, IdempotencyKeyStatus.Completed, ttlHours);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKey(key));
    }

    private async Task UpdateRecordAsync(
        string key, int statusCode, string? body, string? contentType, IdempotencyKeyStatus status, int ttlHours)
    {
        var db = redis.GetDatabase();
        var redisKey = RedisKey(key);
        var fullTtl = TimeSpan.FromHours(ttlHours);

        var existing = await db.StringGetAsync(redisKey);
        var record = existing.IsNullOrEmpty
            ? new IdempotencyKeyRecord { Key = key, CreatedAt = DateTimeOffset.UtcNow }
            : JsonSerializer.Deserialize<IdempotencyKeyRecord>((string)existing!, jsonOptions) ?? new IdempotencyKeyRecord { Key = key };

        record.Status = status;
        record.ResponseStatusCode = statusCode;
        record.ResponseBody = body;
        record.ResponseContentType = contentType;

        // 完成後換成完整的長 TTL，覆蓋原本的短鎖定 TTL
        var value = JsonSerializer.Serialize(record, jsonOptions);
        await db.StringSetAsync(redisKey, value, fullTtl);
    }
}
