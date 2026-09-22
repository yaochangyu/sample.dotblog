using Lab.API.Signature.Models;
using Microsoft.EntityFrameworkCore;

namespace Lab.API.Signature.Data;

public class SignatureDbContext : DbContext
{
    public SignatureDbContext(DbContextOptions<SignatureDbContext> options) : base(options)
    {
    }

    public DbSet<ApiKeyClient> ApiKeyClients => Set<ApiKeyClient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiKeyClient>(entity =>
        {
            entity.ToTable("ApiKeyClients");
            entity.HasKey(e => e.ApiKey);
            entity.Property(e => e.ApiKey).HasMaxLength(128);
            entity.Property(e => e.Secret).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ClientName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // ⚠️ 僅供教學展示的 demo 假資料，Secret 為明碼，正式環境嚴禁比照使用真實 partner secret。
            entity.HasData(
                new ApiKeyClient
                {
                    ApiKey = "demo-api-key-001",
                    Secret = "demo-secret-001-please-change",
                    ClientName = "Demo Partner One",
                    CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new ApiKeyClient
                {
                    ApiKey = "demo-api-key-002",
                    Secret = "demo-secret-002-please-change",
                    ClientName = "Demo Partner Two",
                    CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                },
                new ApiKeyClient
                {
                    ApiKey = "demo-api-key-003",
                    Secret = "demo-secret-003-please-change",
                    ClientName = "Demo Partner Three",
                    CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                });
        });
    }
}
