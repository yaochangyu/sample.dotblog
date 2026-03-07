using IdempotencyKey.WebApi.IdempotencyKeys;
using Microsoft.EntityFrameworkCore;

namespace IdempotencyKey.WebApi.Members;

public class MemberDbContext(DbContextOptions<MemberDbContext> options) : DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<IdempotencyKeyRecord> IdempotencyKeys => Set<IdempotencyKeyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Email).IsRequired().HasMaxLength(254);
            entity.HasIndex(m => m.Email).IsUnique();
        });

        modelBuilder.Entity<IdempotencyKeyRecord>(entity =>
        {
            entity.HasKey(k => k.Key);
            entity.Property(k => k.Key).HasMaxLength(255);
            entity.Property(k => k.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(k => k.RequestFingerprint).HasMaxLength(64);
            entity.HasIndex(k => k.ExpiresAt); // 加速過期清理查詢
        });
    }
}
