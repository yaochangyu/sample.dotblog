﻿using System;
using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain;
using Lab.ChangeTracking.Domain.EmployeeAggregate;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;
using Lab.ChangeTracking.Infrastructure.DB;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.MultiTestCase.UnitTest;

// assistant
internal class TestAssistants
{
    private static IServiceProvider _serviceProvider;

    public static IDbContextFactory<EmployeeDbContext> EmployeeDbContextFactory =>
        _serviceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();

    public static IEmployeeRepository EmployeeRepository =>
        _serviceProvider.GetService<IEmployeeRepository>();

    public static IEmployeeAggregate<IEmployeeEntity> EmployeeAggregate =>
        _serviceProvider.GetService<IEmployeeAggregate<IEmployeeEntity>>();

    static TestAssistants()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        SetTestEnvironmentVariable();
    }

    public static void ConfigureTestServices(IServiceCollection services)
    {
        services.AddAppEnvironment();
        services.AddEntityFramework();
        
        services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
        services.AddSingleton<IAccessContext, AccessContext>();
        // services.AddSingleton<IEmployeeAggregate, EmployeeAggregate>();
        _serviceProvider = services.BuildServiceProvider();
    }

    public static void SetTestEnvironmentVariable()
    {
        var option = _serviceProvider.GetService<AppEnvironmentOption>();
        option.EmployeeDbConnectionString =
            "Data Source=localhost;Initial Catalog=EmployeeDb;Integrated Security=false;User ID=sa;Password=pass@w0rd1~;MultipleActiveResultSets=True;TrustServerCertificate=True";
    }
    public static Guid Parse(string id)
    {
        var guidFormat = "{0}-0000-0000-0000-000000000000";
        var guidText = string.Format(guidFormat, id.PadRight(8, '0'));
        var key = Guid.Parse(guidText);
        return key;
    }
}