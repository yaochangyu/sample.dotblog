using System;
using Lab.SQLite.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lab.SQLite
{
    public class EmployeeContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
    {
        public EmployeeDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("由設計工具產生 Database，初始化 DbContextOptionsBuilder");

            var config = DefaultDbContextManager.Configuration;
            var connectionString = config.GetConnectionString("DefaultConnection");
            
            Console.WriteLine($"由 appsettings.json 讀取連線字串為：{connectionString}");
        
            var optionsBuilder = new DbContextOptionsBuilder<EmployeeDbContext>();
            optionsBuilder.UseSqlite(connectionString);
            Console.WriteLine($"DbContextOptionsBuilder 設定完成");

            return new EmployeeDbContext(optionsBuilder.Options);
        }
    }
}