using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lab.DAL
{
    public class DbContextOptionManager
    {
        public  static DbContextOptions<EmployeeContext> CreateEmployeeDbContextOptions()
        {
            var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json")
                                .Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var loggerFactory = LoggerFactory.Create(builder =>
                                                     {
                                                         builder
                                                             //.AddFilter("Microsoft",                 LogLevel.Warning)
                                                             //.AddFilter("System",                    LogLevel.Warning)
                                                             .AddFilter("Lab.DAL", LogLevel.Debug)
                                                             .AddConsole()
                                                             ;
                                                     });
            return new DbContextOptionsBuilder<EmployeeContext>()
                   .UseSqlServer(connectionString)
                   .UseLoggerFactory(loggerFactory)
                   .Options;
        } 
    }
}