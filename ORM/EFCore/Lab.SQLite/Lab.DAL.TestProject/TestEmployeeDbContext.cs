using System;
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
                Console.WriteLine($"設定連線字串:{connectionString}");
                optionsBuilder.UseSqlite(connectionString);
            }
        }

        //管理索引
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}