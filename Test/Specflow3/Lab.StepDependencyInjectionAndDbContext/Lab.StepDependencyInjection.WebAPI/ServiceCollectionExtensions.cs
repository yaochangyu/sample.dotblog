using Lab.StepDependencyInjection.WebAPI.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.StepDependencyInjection.WebAPI;

public static class ServiceCollectionExtension
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton(p => new AppSettings
        {
            ConnectionString = "Host=localhost;Port=5432;Database=employee;Username=postgres;Password=guest"
        });
        services.AddDbContextFactory<EmployeeDbContext>((provider, builder) =>
        {
            var appSettings = provider.GetService<AppSettings>();
            var connectionString = appSettings.ConnectionString;
            builder.UseNpgsql(connectionString);
        });
        return services;
    }
}