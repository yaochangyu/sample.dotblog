using Lab.NETMiniProfiler.Infrastructure.EFCore5.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.NETMiniProfiler.Infrastructure.EFCore5;

public static class AppDependencyInjectionExtensions
{
    public static void AddAppEnvironment(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        services.AddSingleton<AppEnvironmentOption>();
    }

    public static void AddEntityFramework(this IServiceCollection services)
    {
        services.AddPooledDbContextFactory<EmployeeDbContext>((provider, optionsBuilder) =>
        {
            var option = provider.GetService<AppEnvironmentOption>();
            var connectionString = option.EmployeeDbConnectionString;
            var loggerFactory = provider.GetService<ILoggerFactory>();
            optionsBuilder.UseSqlServer(connectionString)
                          .UseLoggerFactory(loggerFactory)
                ;
        });

        ;

        // services.AddPooledDbContextFactory<EmployeeDbContext>((provider, options) =>
        // {
        //     var option = provider.GetService<AppEnvironmentOption>();
        //     var loggerFactory = provider.GetService<ILoggerFactory>();
        //     options.UseNpgsql(
        //                option.MemberDbConnectionString, //只會呼叫一次
        //                builder =>
        //                    builder.EnableRetryOnFailure(
        //                        10,
        //                        TimeSpan.FromSeconds(30),
        //                        new List<string> { "57P01" }))
        //
        //            //  .UseLazyLoadingProxies()
        //            .UseSnakeCaseNamingConvention()
        //            .UseLoggerFactory(loggerFactory)
        //         ;
        // });
    }

}