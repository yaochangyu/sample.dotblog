using Lab.Creds.Proof.Signatures;
using Microsoft.EntityFrameworkCore;

namespace Lab.Creds.Proof;

public sealed class ProofDbContext(DbContextOptions<ProofDbContext> options) : DbContext(options)
{
    public DbSet<RegisteredSigningKey> SigningKeys => Set<RegisteredSigningKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseOpenIddict();
        modelBuilder.Entity<RegisteredSigningKey>(key =>
        {
            key.ToTable("request_signing_keys");
            key.HasKey(k => k.KeyId);
            key.Property(k => k.KeyId).HasMaxLength(128);
            key.Property(k => k.ClientId).HasMaxLength(128);
            key.Property(k => k.Algorithm).HasMaxLength(64);
            key.HasIndex(k => k.ClientId);
        });
    }
}
