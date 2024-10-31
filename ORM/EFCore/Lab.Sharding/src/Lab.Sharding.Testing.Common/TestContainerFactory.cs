using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Lab.Sharding.Testing.Common;

public class TestContainerFactory
{
	public static async Task<IContainer> CreateMockServerContainerAsync()
	{
		var container = new ContainerBuilder()
			.WithName("mockserver")
			.WithImage("mockserver/mockserver")
			.WithPortBinding(1080, true)
			.Build();
		await container.StartAsync();
		return container;
	}

	public static async Task<MsSqlContainer> CreateMsSqlContainerAsync()
	{
		var container = new MsSqlBuilder()
			.WithName("sql2019")
			.WithImage("mcr.microsoft.com/mssql/server:2019-latest")
			.WithPassword("pass@w0rd1~")
			.WithEnvironment("ACCEPT_EULA", "Y")
			.WithEnvironment("MSSQL_PID", "Developer")
			.WithPortBinding(1433, true)
			.Build();
		await container.StartAsync();
		return container;
	}

	public static async Task<PostgreSqlContainer> CreatePostgreSqlContainerAsync()
	{
		var waitStrategy = Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready");
		var container = new PostgreSqlBuilder()
			.WithImage("postgres:13-alpine")
			.WithName("postgres.13")
			.WithPortBinding(5432, true)
			.WithWaitStrategy(waitStrategy)
			.WithUsername("postgres")
			.WithPassword("postgres")
			.Build();
		await container.StartAsync();
		return container;
	}

	// TODO:docker hub 有訪問次數限制，需要一台 proxy server
	public static async Task<RedisContainer> CreateRedisContainerAsync()
	{
		var redisContainer = new RedisBuilder()
			.WithImage("redis:7.0")
			.Build();
		await redisContainer.StartAsync();
		return redisContainer;
	}

	public static string GetMockServerConnection(IContainer container)
	{
		var port = container.GetMappedPublicPort(1080);
		return $"http://{container.Hostname}:{port}";
	}
}