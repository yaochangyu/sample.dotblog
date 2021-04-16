using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;

namespace Lab.SQLite.EntityModel
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
                    var memoryOptions          = options.FindExtension<InMemoryOptionsExtension>();
                    var sqliteOptionsExtension = options.FindExtension<SqliteOptionsExtension>();

                    if (sqliteOptionsExtension != null)
                    {
                        Console.WriteLine($"EmployeeDbContext 的連線字串為:{sqliteOptionsExtension.ConnectionString}，執行 Migration");
                    }

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
            //     Console.WriteLine("OnConfiguring");
            //     optionsBuilder.UseSqlite(connectionString);
            // }
        }

        //管理索引
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("設定資料表定義");
        }
    }
}