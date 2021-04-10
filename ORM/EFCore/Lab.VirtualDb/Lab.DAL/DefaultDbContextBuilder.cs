using System;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.DAL
{
    public class DefaultDbContextBuilder
    {
        private IServiceProvider _serviceProvider;

        public DefaultDbContextBuilder(IServiceCollection services)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            services.AddDbContextFactory<EmployeeContext>(ApplyConfigurePhysical);
            this._serviceProvider = services.BuildServiceProvider();
            services.AddSingleton<EmployeeContext>();
        }

        public static void ApplyConfigureMemory(IServiceProvider        provider,
                                                DbContextOptionsBuilder optionsBuilder)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
                                                     {
                                                         builder

                                                             //.AddFilter("Microsoft",                 LogLevel.Warning)
                                                             //.AddFilter("System",                    LogLevel.Warning)
                                                             .AddFilter("Lab.DAL", LogLevel.Debug)
                                                             .AddConsole()
                                                             ;
                                                     });
            optionsBuilder.UseInMemoryDatabase("Demo")
                          .UseLoggerFactory(loggerFactory)
                ;
        }

        public static void ApplyConfigurePhysical(IServiceProvider        provider,
                                                  DbContextOptionsBuilder optionsBuilder)
        {
            var configuration    = provider.GetService<IConfiguration>();
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
            optionsBuilder.UseSqlServer(connectionString)
                          .UseLoggerFactory(loggerFactory)
                ;
        }

        public static DbContextOptions<EmployeeContext> CreateEmployeeDbContextOptions()
        {
            return CreateEmployeeDbContextOptionsBuilder().Options;
        }

        public static DbContextOptionsBuilder<EmployeeContext> CreateEmployeeDbContextOptionsBuilder()
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
                ;
        }
    }
}