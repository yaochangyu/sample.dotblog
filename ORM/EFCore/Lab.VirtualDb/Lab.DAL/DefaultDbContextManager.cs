using System;
using System.IO;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.DAL
{
    internal class DefaultDbContextManager
    {
        public static readonly  bool[]                Migrated = {false};
        private static readonly Lazy<ServiceProvider> s_serviceProviderLazy;
        private static readonly Lazy<IConfiguration>  s_configurationLazy;
        private static readonly ILoggerFactory        s_loggerFactory;
        private static          ServiceProvider       s_serviceProvider;
        private static          IConfiguration        s_configuration;
        private static          DateTime?             s_now;

        public static DateTime Now
        {
            get
            {
                if (s_now == null)
                {
                    return DateTime.UtcNow;
                }

                return s_now.Value;
            }
            set => s_now = value;
        }

        public static ServiceProvider ServiceProvider
        {
            get
            {
                if (s_serviceProvider == null)
                {
                    s_serviceProvider = s_serviceProviderLazy.Value;
                }

                return s_serviceProvider;
            }
            set => s_serviceProvider = value;
        }

        public static IConfiguration Configuration
        {
            get
            {
                if (s_configuration == null)
                {
                    s_configuration = s_configurationLazy.Value;
                }

                return s_configuration;
            }
            set => s_configuration = value;
        }

        private static ServiceCollection s_services;
        static DefaultDbContextManager()
        {
            s_services = new ServiceCollection();
            
            s_serviceProviderLazy =
                new Lazy<ServiceProvider>(() =>
                                          {
                                              var services = s_services;
                                              services.AddDbContextFactory<EmployeeDbContext>(ApplyConfigurePhysical);
                                              return services.BuildServiceProvider();
                                          });
            s_configurationLazy
                = new Lazy<IConfiguration>(() =>
                                           {
                                               var configBuilder = new ConfigurationBuilder()
                                                                   .SetBasePath(Directory.GetCurrentDirectory())
                                                                   .AddJsonFile("appsettings.json");
                                               return configBuilder.Build();
                                           });
            s_loggerFactory = LoggerFactory.Create(builder =>
                                                   {
                                                       builder

                                                           //.AddFilter("Microsoft",                 LogLevel.Warning)
                                                           //.AddFilter("System",                    LogLevel.Warning)
                                                           .AddFilter("Lab.DAL", LogLevel.Debug)
                                                           .AddConsole()
                                                           ;
                                                   });
        }

        private static void ApplyConfigureMemory(IServiceProvider        provider,
                                                 DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("Demo")
                          .UseLoggerFactory(s_loggerFactory)
                ;
        }

        private static void ApplyConfigurePhysical(IServiceProvider        provider,
                                                   DbContextOptionsBuilder optionsBuilder)
        {
            var config = provider.GetService<IConfiguration>();
            if (config == null)
            {
                config = Configuration;
            }

            var connectionString = config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString)
                          .UseLoggerFactory(s_loggerFactory)
                ;
        }

        public static T GetInstance<T>()
        {
            return ServiceProvider.GetService<T>();
        }
   
        public static void SetPhysicalDatabase<TContext>() where TContext : DbContext
        {
            var services = s_services;
            services.AddDbContextFactory<TContext>(ApplyConfigurePhysical);
            ServiceProvider = services.BuildServiceProvider();
        }

        public static void SetMemoryDatabase<TContext>() where TContext : DbContext
        {
            var services = s_services;
            services.AddDbContextFactory<TContext>(ApplyConfigureMemory);
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}