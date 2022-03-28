using Lab.ORM.DynamicField.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.ORM.DynamicField;

public static class AppDependencyInjectionExtensions
{
    public static void AddAppEnvironment(this IServiceCollection services)
    {
        services.AddLogging(builder => { builder.AddConsole(); });
        services.AddSingleton<AppEnvironmentOption>();
    }

    public static void AddEntityFramework(this IServiceCollection services)
    {
        services.AddDbContextFactory<EmployeeDbContext>((provider, options) =>
        {
            var option = provider.GetService<AppEnvironmentOption>();
            var connectionString = option.EmployeeDbConnectionString;
            options.UseNpgsql(connectionString, //只會呼叫一次
                              builder => builder.EnableRetryOnFailure(
                                  10,
                                  TimeSpan.FromSeconds(30),
                                  new List<string> { "57P01" })
                )

                //  .UseLazyLoadingProxies()
                .EnableSensitiveDataLogging()                   //这将捕获通过迁移发送的更改。
                .LogTo(Console.WriteLine, LogLevel.Information) //这将捕获所有发送到数据库的SQL。
                // .UseLoggerFactory(loggerFactory)
                ;

            //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }

    // public static void AddEntityFramework(this IServiceCollection services)
    // {
    //     services.AddPooledDbContextFactory<EmployeeDbContext>((provider, optionsBuilder) =>
    //     {
    //         var option = provider.GetService<AppEnvironmentOption>();
    //         var connectionString = option.EmployeeDbConnectionString;
    //         var loggerFactory = provider.GetService<ILoggerFactory>();
    //         optionsBuilder.UseNpgsql(connectionString)
    //                       .UseLoggerFactory(loggerFactory)
    //             ;
    //     });
    // }
}