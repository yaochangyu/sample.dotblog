using IdempotencyKey.WebApi.Members;
using Microsoft.EntityFrameworkCore;

namespace IdempotencyKey.WebApi.IdempotencyKeys;

public class EfIdempotencyKeyStore(MemberDbContext db) : IIdempotencyKeyStore
{
    public async Task<IdempotencyKeyRecord?> TryAcquireAsync(
        string key, string fingerprint, int ttlHours, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new IdempotencyKeyRecord
        {
            Key = key,
            Status = IdempotencyKeyStatus.InProgress,
            RequestFingerprint = fingerprint,
            CreatedAt = now,
            ExpiresAt = now.AddHours(ttlHours)
        };

        try
        {
            db.IdempotencyKeys.Add(record);
            await db.SaveChangesAsync(ct);
            return null; // 成功取得鎖
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            db.ChangeTracker.Clear();

            // Key 已存在，回傳現有記錄
            return await db.IdempotencyKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Key == key, ct);
        }
    }

    public async Task SetCompletedAsync(
        string key, int statusCode, string? body, string? contentType, CancellationToken ct = default)
    {
        await db.IdempotencyKeys
            .Where(k => k.Key == key)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.Status, IdempotencyKeyStatus.Completed)
                .SetProperty(k => k.ResponseStatusCode, statusCode)
                .SetProperty(k => k.ResponseBody, body)
                .SetProperty(k => k.ResponseContentType, contentType), ct);
    }

    public async Task SetFailedAsync(
        string key, int statusCode, string? body, string? contentType, CancellationToken ct = default)
    {
        await db.IdempotencyKeys
            .Where(k => k.Key == key)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.Status, IdempotencyKeyStatus.Failed)
                .SetProperty(k => k.ResponseStatusCode, statusCode)
                .SetProperty(k => k.ResponseBody, body)
                .SetProperty(k => k.ResponseContentType, contentType), ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await db.IdempotencyKeys
            .Where(k => k.Key == key)
            .ExecuteDeleteAsync(ct);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
}
