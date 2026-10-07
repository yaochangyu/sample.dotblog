using Microsoft.EntityFrameworkCore;

namespace Lab.Creds.Proof;

public sealed class ProofDbContext(DbContextOptions<ProofDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.UseOpenIddict();
}
