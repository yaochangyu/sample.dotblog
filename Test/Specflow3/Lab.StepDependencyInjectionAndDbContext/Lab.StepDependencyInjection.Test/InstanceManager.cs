using Lab.StepDependencyInjection.WebAPI.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.StepDependencyInjection.Test;

internal class InstanceManager
{
    public static IServiceCollection InitializeServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FileProvider>();

        services.AddLogging(p => p.AddConsole());
        services.AddDbContextFactory<EmployeeDbContext>((p, options) =>
        {
            var loggerFactory = p.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("測試");
            logger.LogInformation("選用測試專案的的注入設定");
            var connectionString =
                "Host=localhost;Port=5432;Database=employee_test;Username=postgres;Password=guest;";

            // var connectionString = sp.GetService<MEMBER_SERVICE_DB_CONN_STR>().Value;
            options.UseNpgsql(connectionString, //只會呼叫一次
                    builder => builder.EnableRetryOnFailure(
                        10,
                        TimeSpan.FromSeconds(30),
                        new List<string> { "57P01" })
                )

                //  .UseLazyLoadingProxies()
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging()
                .UseLoggerFactory(loggerFactory)
                ;

            //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }, lifetime: ServiceLifetime.Transient);
        return services;
    }
}