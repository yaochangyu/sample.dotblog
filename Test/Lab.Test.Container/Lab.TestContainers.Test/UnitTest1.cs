using System.Data.Common;
using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Lab.TestContainers.Test;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task GenericContainer()
    {
        var waitStrategy = Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready");
        var postgreSqlContainer = new ContainerBuilder()
            .WithImage("postgres:12-alpine")
            .WithName("postgres.12")
            .WithPortBinding(5432)
            .WithWaitStrategy(waitStrategy)
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .Build();
        await postgreSqlContainer.StartAsync()
            .ConfigureAwait(false);

        var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres";
        await using DbConnection connection = new NpgsqlConnection(connectionString);
        await using DbCommand command = new NpgsqlCommand();
        await connection.OpenAsync();
        command.Connection = connection;
        command.CommandText = "SELECT 1";
        var reader = await command.ExecuteReaderAsync();
        while (true)
        {
            var hasData = await reader.ReadAsync();
            if (hasData == false)
            {
                break;
            }

            var value = reader.GetValue(0);
        }
    }

    [TestMethod]
    public async Task ModuleContainer()
    {
        var waitStrategy = Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready");
        var postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:12-alpine")
            .WithName("postgres.12")
            .WithPortBinding(5432, assignRandomHostPort: true)
            .WithWaitStrategy(waitStrategy)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await postgreSqlContainer.StartAsync()
            .ConfigureAwait(false);

        var connectionString = postgreSqlContainer.GetConnectionString();
        await using DbConnection connection = new NpgsqlConnection(connectionString);
        await using DbCommand command = new NpgsqlCommand();
        await connection.OpenAsync();
        command.Connection = connection;
        command.CommandText = "SELECT 1";
    }

    [TestMethod]
    public async Task CreateImage()
    {
        var solutionDirectory = CommonDirectoryPath.GetSolutionDirectory();
        var dockerFilePath = "Lab.TestContainers.WebApi/Dockerfile";
        var imageBuilder = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(solutionDirectory, string.Empty)
            .WithDockerfile(dockerFilePath)
            .WithName("my.aspnet.core.7")
            .Build();

        await imageBuilder.CreateAsync()
            .ConfigureAwait(false);
        var container = new ContainerBuilder()
                .WithImage("my.aspnet.core.7")
                .WithName("my.aspnet.core.7")
                .WithPortBinding(80)
                .Build()
            ;
        await container.StartAsync()
            .ConfigureAwait(false);
        ;
        var scheme = "http";
        var host = "localhost";
        var port = container.GetMappedPublicPort(80);
        var url = "demo";

        var httpClient = new HttpClient();
        var requestUri = new UriBuilder(scheme, host, port, url).Uri;
        var actual = await httpClient.GetStringAsync(requestUri);
        Assert.AreEqual("OK~", actual);
    }
}