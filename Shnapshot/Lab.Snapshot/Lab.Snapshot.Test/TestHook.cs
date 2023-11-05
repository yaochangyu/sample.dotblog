using Lab.Snapshot.DB;
using Lab.Snapshot.WebAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.Snapshot.Test;

[TestClass]
public class TestHook
{
    private static readonly IServiceCollection s_services = new ServiceCollection();
    static IServiceProvider s_serviceProvider;

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        Console.WriteLine("AssemblyInitialize");
        Environment.SetEnvironmentVariable(EnvironmentNames.DbConnectionString, TestAssistant.DbConnectionString);
        ServiceConfiguration.ConfigDb(s_services);
        s_serviceProvider = s_services.BuildServiceProvider();
        using var dbContext = s_serviceProvider.GetService<IDbContextFactory<MemberDbContext>>().CreateDbContext();

        // drop and create database
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Console.WriteLine("AssemblyCleanup");

        // drop database
        using var dbContext = s_serviceProvider.GetService<IDbContextFactory<MemberDbContext>>().CreateDbContext();

        // dbContext.Database.EnsureDeleted();
    }
}