using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace Lab.ChangeTracking.Infrastructure.DB.EntityModel
{
    public class EmployeeDbContext : DbContext
    {
        private static readonly bool[] s_migrated = { false };

        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Identity> Identities { get; set; }

        public virtual DbSet<OrderHistory> OrderHistories { get; set; }

        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
            : base(options)
        {
            if (s_migrated[0])
            {
                return;
            }

            lock (s_migrated)
            {
                if (s_migrated[0] == false)
                {
                    var memoryOptions = options.FindExtension<InMemoryOptionsExtension>();

                    if (memoryOptions == null)
                    {
                        var sqlOptions = options.FindExtension<SqlServerOptionsExtension>();
                        if (sqlOptions != null)
                        {
                            this.Database.Migrate();
                        }
                    }

                    s_migrated[0] = true;
                }
            }
        }

        //管理索引
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(p =>
            {
                p.HasKey(e => e.Id)
                 .IsClustered(false);

                p.HasIndex(e => e.SequenceId)
                 .IsUnique()
                 .IsClustered();

                p.Property(p => p.Remark)
                 .IsRequired(false)
                    ;
            });

            modelBuilder.Entity<Identity>(p =>
            {
                p.HasKey(e => e.Employee_Id)
                 .IsClustered(false);

                p.HasIndex(e => e.SequenceId)
                 .IsUnique()
                 .IsClustered();
                
                p.Property(p => p.Remark)
                 .IsRequired(false)
                    ;
            });
        }
    }
}