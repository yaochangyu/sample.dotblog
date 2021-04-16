using System;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lab.DAL
{
    public class EmployeeContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
    {
        public EmployeeDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("EmployeeContextFactory - 由設計工具產生 Database，初始化 DbContextOptionsBuilder");

            var config = DefaultDbContextManager.Configuration;
            var connectionString = config.GetConnectionString("DefaultConnection");
            
            Console.WriteLine($"EmployeeContextFactory - 讀取 appsettings.json 檔案的讀取連線字串為：{connectionString}");
        
            var optionsBuilder = new DbContextOptionsBuilder<EmployeeDbContext>();
            optionsBuilder.UseSqlite(connectionString);
            Console.WriteLine($"EmployeeContextFactory - DbContextOptionsBuilder 設定完成");

            return new EmployeeDbContext(optionsBuilder.Options);
        }
    }
}