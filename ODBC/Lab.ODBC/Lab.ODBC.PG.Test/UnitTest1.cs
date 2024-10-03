using System.Data.Odbc;
using Dapper;
using Lab.ODBC.PG.Test.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ODBC.PG.Test;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task Read()
    {
        var connectionString =
            "Driver={PostgreSQL Unicode};Server=localhost;Port=5432;Database=employee;Uid=postgres;Pwd=postgres;";
        await using var connection = new OdbcConnection(connectionString);
        await using var command = new OdbcCommand();
        await connection.OpenAsync();
        command.Connection = connection;
        command.CommandText = @"select * from ""Employee""";
        await using var reader = await command.ExecuteReaderAsync();

        while (true)
        {
            var hasData = await reader.ReadAsync();
            if (hasData == false)
            {
                break;
            }

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                var value = reader.GetValue(i);
                Console.WriteLine($"{name}: {value}");
            }
        }

        await connection.CloseAsync();
    }

    [TestMethod]
    public async Task ReadForDapper()
    {
        var connectionString =
            "Driver={PostgreSQL Unicode};Server=localhost;Port=5432;Database=employee;Uid=postgres;Pwd=postgres;";
        await using var connection = new OdbcConnection(connectionString);
        var sql = @"select * from ""Employee""";
        var data = connection.Query<Employee>(sql).ToList();
        await connection.CloseAsync();
    }

    [TestMethod]
    public async Task ReadForEFODBC()
    {
        await using var dbContext = CreateDbContext();

        var employees = await dbContext.Employees.AsTracking().ToListAsync();
    }

    private static EmployeeDbContext CreateDbContext()
    {
        // var connectionString =
        //     "Driver={PostgreSQL Unicode};Server=localhost;Port=5432;Database=employee;Uid=postgres;Pwd=postgres";
        var connectionString =
            "Host=localhost;Port=5432;Database=employee;Username=postgres;Password=postgres";
        var builder = new DbContextOptionsBuilder<EmployeeDbContext>();
        builder.UseNpgsql(connectionString);
        var dbContextOptions = builder
            .Options;

        var dbContext = new EmployeeDbContext(dbContextOptions);
        dbContext.Database.SetConnectionString(connectionString);
        return dbContext;
    }
}