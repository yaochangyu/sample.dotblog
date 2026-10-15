using Microsoft.EntityFrameworkCore;

namespace AuthSpike.Data;

/// <summary>OpenIddict 的 EF Core 儲存區；spike 使用 InMemory 或 PostgreSQL。</summary>
public sealed class SpikeDbContext(DbContextOptions<SpikeDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();
    }
}
