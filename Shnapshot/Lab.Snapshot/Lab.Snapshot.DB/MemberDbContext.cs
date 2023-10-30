using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace Lab.Snapshot.DB;

public class MemberDbContext : DbContext
{
    public DbSet<MemberDataEntity> Members { get; set; }

    public DbSet<SnapshotDataEntity> Snapshots { get; set; }

    public MemberDbContext(DbContextOptions<MemberDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence("member_collection_seqno")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.ApplyConfiguration(new MemberConfiguration());
    }

    internal class MemberConfiguration : IEntityTypeConfiguration<MemberDataEntity>
    {
        public void Configure(EntityTypeBuilder<MemberDataEntity> builder)
        {
            builder.ToTable("Member");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Accounts).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.Profile).HasColumnType("jsonb").IsRequired(false);
            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(50).IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Version).IsRequired();

            // indexes
            builder.HasIndex(p => new { p.Id, p.Version }).IsUnique();
            builder.HasIndex(x => x.Accounts).HasMethod("GIN");
        }
    }
    
    internal class SnapshotConfiguration : IEntityTypeConfiguration<SnapshotDataEntity>
    {
        public void Configure(EntityTypeBuilder<SnapshotDataEntity> builder)
        {
            builder.ToTable("Snapshot");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Data).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(50).IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Version).IsRequired();

            // indexes
            builder.HasIndex(p => new { p.Id, p.Version }).IsUnique();
            builder.HasIndex(x => x.Data).HasMethod("GIN");
        }
    }

}