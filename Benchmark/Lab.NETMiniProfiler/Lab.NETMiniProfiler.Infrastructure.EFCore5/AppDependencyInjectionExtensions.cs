using Lab.NETMiniProfiler.Infrastructure.EFCore5.EntityModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace Lab.NETMiniProfiler.Infrastructure.EFCore5;
public static class AppDependencyInjectionExtensions
{
    public static void AddAppEnvironment(this IServiceCollection services)
    {
        services.AddLogging(builder => { builder.AddConsole(); });
        services.AddSingleton<AppEnvironmentOption>();
    }

    public static void AddEntityFramework(this IServiceCollection services)
    {
        // services.AddPooledDbContextFactory<EmployeeDbContext>((provider, optionsBuilder) =>
        // {
        //     var option = provider.GetService<AppEnvironmentOption>();
        //     var connectionString = option.EmployeeDbConnectionString;
        //     var loggerFactory = provider.GetService<ILoggerFactory>();
        //     optionsBuilder.UseSqlServer(connectionString)
        //                   .UseLoggerFactory(loggerFactory)
        //         ;
        // });

        services.AddPooledDbContextFactory<EmployeeDbContext>((provider, optionsBuilder) =>
        {
            // var mssqlOptions = optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>();
            // var npgsqlOptions = optionsBuilder.Options.FindExtension<NpgsqlOptionsExtension>();

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