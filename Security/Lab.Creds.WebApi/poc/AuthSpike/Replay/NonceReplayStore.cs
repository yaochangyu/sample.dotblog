using Microsoft.EntityFrameworkCore;

namespace AuthSpike.Replay;

/// <summary>防重放狀態無法可靠讀寫（lab 以 SimulateOutage 模擬）；呼叫端必須以 503 明確回報，不得靜默放行。</summary>
public sealed class ReplayStateUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// 防重放 nonce 紀錄（lab：EF Core InMemory）。同一個實例可同時供多個建立訂單 API 執行個體使用，模擬共用儲存。
/// 判斷與登錄以同一把鎖序列化，確保併發下同一 nonce 只會成功登錄一次。
/// 保存期（lab 暫定）：至簽章有效期加時鐘容差；已過保存期的紀錄於下次登錄時清除。
/// </summary>
public sealed class NonceReplayStore
{
    private readonly object _gate = new();
    private readonly DbContextOptions<NonceDbContext> _options = new DbContextOptionsBuilder<NonceDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
        .Options;

    /// <summary>lab 故障模擬：為 true 時所有讀寫都視為無法可靠完成。</summary>
    public bool SimulateOutage { get; set; }

    /// <summary>嘗試登錄 (clientId, nonce)；回傳 true 表示首次登錄成功，false 表示已存在（重放）。</summary>
    public bool TryRegister(string clientId, string nonce, DateTimeOffset retainUntil)
    {
        lock (_gate)
        {
            try
            {
                if (SimulateOutage)
                {
                    throw new InvalidOperationException("模擬防重放儲存故障。");
                }

                using var db = new NonceDbContext(_options);
                var now = DateTimeOffset.UtcNow;
                db.Nonces.RemoveRange(db.Nonces.Where(entry => entry.RetainUntil < now).ToList());
                if (db.Nonces.Any(entry => entry.ClientId == clientId && entry.Nonce == nonce))
                {
                    db.SaveChanges();
                    return false;
                }

                db.Nonces.Add(new NonceEntry { ClientId = clientId, Nonce = nonce, RetainUntil = retainUntil });
                db.SaveChanges();
                return true;
            }
            catch (Exception exception) when (exception is not ReplayStateUnavailableException)
            {
                throw new ReplayStateUnavailableException("防重放狀態無法讀寫。", exception);
            }
        }
    }

    /// <summary>查詢紀錄的保存至時間（僅供測試驗證保存期；紀錄不存在時回傳 null）。</summary>
    public DateTimeOffset? RetainUntilOf(string clientId, string nonce)
    {
        lock (_gate)
        {
            using var db = new NonceDbContext(_options);
            return db.Nonces
                .Where(entry => entry.ClientId == clientId && entry.Nonce == nonce)
                .Select(entry => (DateTimeOffset?)entry.RetainUntil)
                .FirstOrDefault();
        }
    }
}

public sealed class NonceEntry
{
    public string ClientId { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public DateTimeOffset RetainUntil { get; set; }
}

public sealed class NonceDbContext(DbContextOptions<NonceDbContext> options) : DbContext(options)
{
    public DbSet<NonceEntry> Nonces => Set<NonceEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<NonceEntry>(entity => entity.HasKey(nameof(NonceEntry.ClientId), nameof(NonceEntry.Nonce)));
}
