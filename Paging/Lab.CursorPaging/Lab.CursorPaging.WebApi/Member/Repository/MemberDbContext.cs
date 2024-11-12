using Microsoft.EntityFrameworkCore;

namespace Lab.CursorPaging.WebApi.Member.Repository;

public class MemberDbContext : DbContext
{
    private static readonly bool[] s_migrated = { false };

    public MemberDbContext(DbContextOptions<MemberDbContext> options) : base(options)
    {
        if (!s_migrated[0])
        {
            lock (s_migrated)
            {
                if (!s_migrated[0])
                {
                    this.Database.Migrate();
                    s_migrated[0] = true;
                }
            }
        }
    }

    public DbSet<MemberDataEntity> Members { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MemberDataEntity>(builder =>
        {
            //property
            builder.ToTable("Member");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Age).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.CreatedBy).IsRequired();
            builder.Property(x => x.UpdatedAt).IsRequired();
            builder.Property(x => x.UpdatedBy).IsRequired();
            builder.Property(p => p.SequenceId).ValueGeneratedOnAdd();

            //index            
            builder.HasIndex(x => x.SequenceId).IsUnique();
        });
    }
}

public class MemberDataEntity : BaseEntity
{
    public string Id { get; set; }

    public string Name { get; set; } = default!;

    public int Age { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long SequenceId { get; set; }
}

public class BaseEntity
{
    public long SequenceId { get; set; }
}