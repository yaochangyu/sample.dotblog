using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.DAL.TestProject
{
    public class TestEmployeeDbContext : DbContext
    {
        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Identity> Identities { get; set; }

        public virtual DbSet<OrderHistory> OrderHistories { get; set; }

        private readonly string _connectionString;

        public TestEmployeeDbContext(string connectionString)
        {
            this._connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = this._connectionString;
            if (optionsBuilder.IsConfigured == false)
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
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