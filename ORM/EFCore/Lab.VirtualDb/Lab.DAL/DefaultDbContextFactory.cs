using System;
using System.IO;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.DAL
{
    internal static class DefaultDbContextFactory
    {
        private static readonly Lazy<ServiceProvider> s_serviceProviderLazy;
        private static readonly Lazy<IConfiguration>  s_configurationLazy;
        private static          ServiceProvider       s_serviceProvider;
        private static          IConfiguration        s_configuration;

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

        static DefaultDbContextFactory()
        {
            s_serviceProviderLazy =
                new Lazy<ServiceProvider>(() =>
                                          {
                                              var services = new ServiceCollection();
                                              services.AddDbContextFactory<EmployeeContext>(ApplyConfigurePhysical);

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
            var configuration = provider.GetService<IConfiguration>();
            if (configuration == null)
            {
                configuration = Configuration;
            }

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

        public static T GetInstance<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public static void SetUseMemoryDatabase()
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<EmployeeContext>(ApplyConfigureMemory);
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}