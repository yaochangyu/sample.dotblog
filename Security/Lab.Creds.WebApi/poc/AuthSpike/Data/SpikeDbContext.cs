using Microsoft.EntityFrameworkCore;

namespace AuthSpike.Data;

/// <summary>OpenIddict 的 EF Core 儲存區；spike 使用 InMemory，正式環境需改為持久化資料庫。</summary>
public sealed class SpikeDbContext(DbContextOptions<SpikeDbContext> options) : DbContext(options);
