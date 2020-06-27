using EFCore3.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EFCore3
{
    public class DbOptionsFactory
    {
        public static DbContextOptions<LabContext> DbContextOptions { get; }

        public static string ConnectionString { get; }
        public static readonly ILoggerFactory MyLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });
        static DbOptionsFactory()
        {
            var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json")
                                .Build();
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
            var loggerFactory = LoggerFactory.Create(builder =>
                                                     {
                                                         builder
                                                             //.AddFilter("Microsoft",                 LogLevel.Warning)
                                                             //.AddFilter("System",                    LogLevel.Warning)
                                                             //.AddFilter("Lab.DAL", LogLevel.Debug)
                                                             .AddConsole()
                                                             ;
                                                     });
            DbContextOptions = new DbContextOptionsBuilder<LabContext>()
                               .UseSqlServer(ConnectionString)
                               .UseLoggerFactory(loggerFactory)
                               .Options;
        }
    }
}