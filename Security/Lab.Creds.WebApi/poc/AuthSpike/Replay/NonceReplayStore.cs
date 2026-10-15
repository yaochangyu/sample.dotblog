using Microsoft.EntityFrameworkCore;

namespace AuthSpike.Replay;

/// <summary>防重放狀態無法可靠讀寫（lab 以 SimulateOutage 或 Redis 連線異常觸發）；呼叫端必須以 503 明確回報，不得靜默放行。</summary>
public sealed class ReplayStateUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// 防重放 nonce 紀錄（InMemory 實作，相容既有單程序/多執行個體模擬）。
/// 實作 INonceReplayStore。
/// </summary>
public sealed class NonceReplayStore : INonceReplayStore
{
    private readonly object _gate = new();
    private readonly DbContextOptions<NonceDbContext> _options = new DbContextOptionsBuilder<NonceDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
        .Options;

    /// <summary>lab 故障模擬：為 true 時所有讀寫都視為無法可靠完成。</summary>
    public bool SimulateOutage { get; set; }

    /// <summary>嘗試登錄 (clientId, nonce)；回傳 true 表示首次登錄成功，false 表示已存在（重放）。同步版本供既有呼叫沿用。</summary>
    public bool TryRegister(string clientId, string nonce, DateTimeOffset retainUntil)
        => TryRegister(clientId, nonce, DateTimeOffset.MinValue, retainUntil) == ReplayRegistration.Registered;

    public ReplayRegistration TryRegister(string clientId, string nonce, DateTimeOffset created, DateTimeOffset retainUntil)
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
                    return ReplayRegistration.Replayed;
                }

                db.Nonces.Add(new NonceEntry { ClientId = clientId, Nonce = nonce, RetainUntil = retainUntil });
                db.SaveChanges();
                return ReplayRegistration.Registered;
            }
            catch (Exception exception) when (exception is not ReplayStateUnavailableException)
            {
                throw new ReplayStateUnavailableException("防重放狀態無法讀寫。", exception);
            }
        }
    }

    public Task<ReplayRegistration> TryRegisterAsync(string clientId, string nonce, DateTimeOffset created, DateTimeOffset retainUntil)
        => Task.FromResult(TryRegister(clientId, nonce, created, retainUntil));

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

    public Task<DateTimeOffset?> RetainUntilOfAsync(string clientId, string nonce)
        => Task.FromResult(RetainUntilOf(clientId, nonce));

    public Task<DateTimeOffset?> GetEpochAsync()
        => Task.FromResult<DateTimeOffset?>(null);
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
