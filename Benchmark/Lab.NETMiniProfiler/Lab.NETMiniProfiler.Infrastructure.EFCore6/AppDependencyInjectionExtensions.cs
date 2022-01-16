using Lab.NETMiniProfiler.Infrastructure.EFCore6.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.NETMiniProfiler.Infrastructure.EFCore6;
public static class AppDependencyInjectionExtensions
{
    public static void AddAppEnvironment(this IServiceCollection services)
    {
        services.AddLogging(builder => { builder.AddConsole(); });
        services.AddSingleton<AppEnvironmentOption>();
    }

    public static void AddEntityFramework(this IServiceCollection services)
    {
        services.AddPooledDbContextFactory<EmployeeDbContext>((provider, optionsBuilder) =>
        {
            var appOption = provider.GetService<AppEnvironmentOption>();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var connectionString = appOption.EmployeeDbConnectionString;
          

            switch (appOption.DatabaseType)
            {
                case DatabaseType.MsSql:
                    optionsBuilder.UseSqlServer(connectionString)
                                  .UseLoggerFactory(loggerFactory);
                    break;
                case DatabaseType.PostgresSQL:
                    optionsBuilder.UseNpgsql(
                                      connectionString, //只會呼叫一次
                                      builder =>
                                          builder.EnableRetryOnFailure(
                                              10,
                                              TimeSpan.FromSeconds(30),
                                              new List<string> { "57P01" }))

                                  // .UseLazyLoadingProxies()
                                  // .UseSnakeCaseNamingConvention()
                                  .UseLoggerFactory(loggerFactory)
                        ;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();    
            }
        });
    }
}