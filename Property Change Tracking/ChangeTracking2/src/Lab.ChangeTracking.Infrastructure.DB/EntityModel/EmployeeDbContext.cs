using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel
{
    public class EmployeeDbContext : DbContext
    {
        private static readonly bool[] s_migrated = { false };

        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Identity> Identity { get; set; }

        public virtual DbSet<Address> Addresses { get; set; }

        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
            : base(options)
        {
            // 改用 CI 執行 Migrate

            // if (s_migrated[0])
            // {
            //     return;
            // }
            //
            // lock (s_migrated)
            // {
            //     if (s_migrated[0] == false)
            //     {
            //         var sqlOptions = options.FindExtension<SqlServerOptionsExtension>();
            //         if (sqlOptions != null)
            //         {
            //             this.Database.Migrate();
            //         }
            //
            //         s_migrated[0] = true;
            //     }
            // }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
            modelBuilder.ApplyConfiguration(new IdentityConfiguration());
            modelBuilder.ApplyConfiguration(new ProfileConfiguration());
        }

        internal class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
        {
            public void Configure(EntityTypeBuilder<Employee> builder)
            {
                builder.ToTable("Employee");
                builder.HasKey(p => p.Id)
                    .IsClustered(false);

                builder.Property(p => p.Name).IsRequired();
                builder.Property(p => p.CreatedBy).IsRequired();
                builder.Property(p => p.CreatedAt).IsRequired();
                builder.Property(p => p.ModifiedAt).IsRequired(false);
                builder.Property(p => p.ModifiedBy).IsRequired(false);
                builder.Property(p => p.Remark).IsRequired(false);

                builder.HasIndex(e => e.SequenceId)
                    .IsUnique()
                    .IsClustered();
            }
        }

        private class IdentityConfiguration : IEntityTypeConfiguration<Identity>
        {
            public void Configure(EntityTypeBuilder<Identity> builder)
            {
                builder.ToTable("Identity");
                builder.HasKey(p => p.Employee_Id).IsClustered(false);
                builder.HasOne(e => e.Employee)
                    .WithOne(p => p.Identity)
                    .HasForeignKey<Identity>(p => p.Employee_Id)
                    .OnDelete(DeleteBehavior.Cascade)
                    ;

                builder.Property(p => p.Account).IsRequired();
                builder.Property(p => p.Password).IsRequired();
                builder.Property(p => p.CreatedBy).IsRequired();
                builder.Property(p => p.CreatedAt).IsRequired();
                builder.Property(p => p.ModifiedAt).IsRequired(false);
                builder.Property(p => p.ModifiedBy).IsRequired(false);
                builder.Property(p => p.Remark).IsRequired(false);

                builder.HasIndex(e => e.SequenceId)
                    .IsUnique()
                    .IsClustered();
            }
        }

        private class ProfileConfiguration : IEntityTypeConfiguration<Address>
        {
            public void Configure(EntityTypeBuilder<Address> builder)
            {
                builder.ToTable("Profile");
                builder.HasKey(p => p.Id).IsClustered(false);
                builder.HasOne(e => e.Employee)
                    .WithMany(p => p.Addresses)
                    .HasForeignKey(p => p.Employee_Id)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.Property(p => p.Country).IsRequired();
                builder.Property(p => p.Street).IsRequired();
                builder.Property(p => p.CreatedBy).IsRequired();
                builder.Property(p => p.CreatedAt).IsRequired();
                builder.Property(p => p.ModifiedBy).IsRequired(false);
                builder.Property(p => p.ModifiedAt).IsRequired(false);
                builder.Property(p => p.Remark).IsRequired(false);

                builder.HasIndex(e => e.SequenceId)
                    .IsUnique()
                    .IsClustered();
            }
        }
    }
}