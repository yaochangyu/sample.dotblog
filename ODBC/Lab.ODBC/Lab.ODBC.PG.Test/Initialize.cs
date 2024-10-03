using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Lab.ODBC.PG.Test.EntityModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lab.ODBC.PG.Test;

[TestClass]
public static class Initialize
{
    static IContainer? PostgreSqlContainer;

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        Console.WriteLine("AssemblyInitialize");
        PostgreSqlContainer = CreatePostgreSQLContainer();
        PostgreSqlContainer.StartAsync().GetAwaiter().GetResult();

        InsertTestData();
    }

    private static void InsertTestData()
    {
        var connectionString = "Host=localhost;Port=5432;Database=employee;Username=postgres;Password=postgres";
        var dbContextOptions = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        using var dbContext = new EmployeeDbContext(dbContextOptions);
        dbContext.Database.EnsureCreated();
        dbContext.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 18,
            Remark = null,
            CreateAt = DateTime.UtcNow,
            CreateBy = "yao"
        });
        dbContext.SaveChanges();
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Console.WriteLine("AssemblyCleanup");
        PostgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        PostgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
    }

    private static IContainer CreatePostgreSQLContainer()
    {
        var waitStrategy = Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready");
        var container = new ContainerBuilder()
            .WithImage("postgres:12-alpine")
            .WithName("postgres.12")
            .WithPortBinding(5432)
            .WithWaitStrategy(waitStrategy)
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "employee")
            .Build();

        return container;
    }
}