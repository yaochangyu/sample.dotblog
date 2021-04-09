using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace Lab.DAL.EntityModel
{
    public class EmployeeContext : DbContext
    {
        private static readonly bool[] s_migrated = {false};

        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Identity> Identities { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        // public EmployeeContext()
        // {
        //     
        // }
        // public EmployeeContext(string connectionString)
        // {
        //     this._connectionString = connectionString;
        //     if (!s_migrated[0])
        //     {
        //         lock (s_migrated)
        //         {
        //             if (!s_migrated[0])
        //             {
        //                 this.Database.Migrate();
        //                 s_migrated[0] = true;
        //             }
        //         }
        //     }
        // }
        public string ConnectionString { get; }

        public EmployeeContext(DbContextOptions<EmployeeContext> options)
            : base(options)
        {
            var sqlServerOptionsExtension = options.FindExtension<SqlServerOptionsExtension>();
            if (sqlServerOptionsExtension != null)
            {
                this.ConnectionString = sqlServerOptionsExtension.ConnectionString;
            }

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
    }
}