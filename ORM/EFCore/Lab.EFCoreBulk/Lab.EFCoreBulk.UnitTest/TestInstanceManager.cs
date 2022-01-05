using System;
using Lab.EFCoreBulk.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.EFCoreBulk.UnitTest;

internal class TestInstanceManager
{
    private static IServiceProvider _serviceProvider;

    public static IDbContextFactory<EmployeeDbContext> EmployeeDbContextFactory =>
        _serviceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();

    static TestInstanceManager()
    {
        ConfigureTestServices();
    }

    public static void ConfigureTestServices()
    {
        var services = new ServiceCollection();
        services.AddAppEnvironment();
        services.AddEntityFramework();
        _serviceProvider = services.BuildServiceProvider();
    }

    public static void SetTestEnvironmentVariable()
    {
        var option = _serviceProvider.GetService<AppEnvironmentOption>();
        option.EmployeeDbConnectionString =
            "Data Source=localhost;Initial Catalog=EmployeeDb;Integrated Security=false;User ID=sa;Password=pass@w0rd1~;MultipleActiveResultSets=True";
    }
}