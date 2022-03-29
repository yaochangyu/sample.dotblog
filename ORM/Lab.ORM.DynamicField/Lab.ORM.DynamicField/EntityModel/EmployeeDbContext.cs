using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Lab.ORM.DynamicField.EntityModel
{
    public class EmployeeDbContext : DbContext
    {
        private static readonly bool[] s_migrated = { false };

        public virtual DbSet<Employee> Employees { get; set; }

        // public virtual DbSet<Identity> Identities { get; set; }
        //
        // public virtual DbSet<OrderHistory> OrderHistories { get; set; }

        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
            : base(options)
        {
        }

        //管理索引
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var options = new JsonSerializerOptions();
            modelBuilder.Entity<Employee>(p =>
            {
                p.Property(p => p.Profiles).HasColumnType("jsonb");
                p.Property(p => p.Customer)
                    .IsRequired(false)
                    .HasColumnType("jsonb")
                    // .HasConversion(p => JsonSerializer.Serialize(p, options),
                    //                p => JsonSerializer.Deserialize<Customer>(p, options))
                    ;

                // p.Property(p => p._customer).HasColumnName("Customer").HasColumnType("jsonb");
                p.Property(p => p.Name).IsRequired().HasMaxLength(50);
                p.Property(p => p.CreatedAt).IsRequired();
                p.Property(p => p.CreatedBy).IsRequired();
                p.Property(p => p.ModifiedAt).IsRequired(false);
                p.Property(p => p.ModifiedBy).IsRequired(false);
                p.Property(p => p.Remark).IsRequired(false);
            });

            modelBuilder.Entity<Identity>(p =>
            {
                p.HasIndex(e => e.SequenceId)
                    .IsUnique()
                    ;
            });
        }
    }
}