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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // optionsBuilder.UseExceptionProcessor();
        optionsBuilder.ConfigureWarnings(b =>
            b.Log((CoreEventId.SaveChangesFailed, LogLevel.Warning),
                (RelationalEventId.CommandError, LogLevel.Warning)));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence("member_collection_seqno")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.ApplyConfiguration(new MemberConfiguration());
        modelBuilder.ApplyConfiguration(new SnapshotConfiguration());
    }

    internal class MemberConfiguration : IEntityTypeConfiguration<MemberDataEntity>
    {
        public void Configure(EntityTypeBuilder<MemberDataEntity> builder)
        {
            builder.ToTable("Member");
            builder.HasKey(p => new
            {
                p.Id
            });
            builder.Property(p => p.Accounts).HasColumnType("jsonb").IsRequired();
            builder.Property(p => p.Profile).HasColumnType("jsonb").IsRequired(false);
            builder.Property(p => p.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(p => p.CreatedBy).HasMaxLength(50).IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Version).IsRequired();

            // indexes
            builder.HasIndex(x => x.Accounts).HasMethod("GIN");
        }
    }

    internal class SnapshotConfiguration : IEntityTypeConfiguration<SnapshotDataEntity>
    {
        public void Configure(EntityTypeBuilder<SnapshotDataEntity> builder)
        {
            builder.ToTable("Snapshot");
            builder.HasKey(x => new
            {
                x.Id,
                x.Version
            });
            builder.Property(x => x.Data).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.DataFormat).IsRequired();
            builder.Property(x => x.DataType).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Version).IsRequired();

            // indexes
            builder.HasIndex(x => x.Data).HasMethod("GIN");
        }
    }
}