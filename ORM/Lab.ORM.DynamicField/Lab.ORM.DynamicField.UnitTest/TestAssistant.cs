using System;
using Lab.ORM.DynamicField.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.ORM.DynamicField.UnitTest;

internal class TestAssistant
{
    private static IServiceProvider _serviceProvider;

    public static IDbContextFactory<EmployeeDbContext> EmployeeDbContextFactory =>
        _serviceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();

    static TestAssistant()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
    }

    public static void ConfigureTestServices(IServiceCollection services)
    {
        services.AddAppEnvironment();
        services.AddEntityFramework();
        _serviceProvider = services.BuildServiceProvider();
    }

    public static void SetTestEnvironmentVariable()
    {
        var connectionString =
            "Host=localhost;Port=5432;Database=employee;Username=postgres;Password=guest;";

        // var connectionString =
        //     "Data Source=localhost;Initial Catalog=EmployeeDb;Integrated Security=false;User ID=sa;Password=pass@w0rd1~;MultipleActiveResultSets=True;TrustServerCertificate=True";

        var option = _serviceProvider.GetService<AppEnvironmentOption>();
        option.EmployeeDbConnectionString = connectionString;
    }
}