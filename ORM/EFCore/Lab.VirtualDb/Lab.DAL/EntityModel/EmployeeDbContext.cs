using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;

namespace Lab.DAL.EntityModel
{
    public class EmployeeDbContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

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
                        this.Database.Migrate();
                    }

                    s_migrated[0] = true;
                }
            }
        }

        // 給 Migration CLI 使用
        // 建構函數配置失敗才需要以下處理
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // var connectionString =
            //     "Server=(localdb)\\mssqllocaldb;Database=Lab.DAL.UnitTest;Trusted_Connection=True;MultipleActiveResultSets=true";
            //
            // // var connectionString = this._connectionString;
            // if (optionsBuilder.IsConfigured == false)
            // {
            //     optionsBuilder.UseSqlServer(connectionString);
            // }
        }

        //管理索引
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(p =>
                                          {
                                              p.HasKey(e => e.Id)
                                               .IsClustered(false);
                                          });

            modelBuilder.Entity<Employee>(p =>
                                          {
                                              p.HasIndex(e => e.SequenceId)
                                               .IsUnique()
                                               .IsClustered();
                                          });
            modelBuilder.Entity<Identity>(p =>
                                          {
                                              p.HasKey(e => e.Employee_Id)
                                               .IsClustered(false);
                                          });
            modelBuilder.Entity<Identity>(p =>
                                          {
                                              p.HasIndex(e => e.SequenceId)
                                               .IsUnique()
                                               .IsClustered();
                                          });
        }
    }
}