using System.Security.Cryptography;
using DotNet.Testcontainers.Containers;
using Npgsql;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace AuthSpike.Tests.Support;

public enum StorageService
{
    AuthDb,
    OrdersDb,
    Redis,
}

/// <summary>
/// 真實儲存的測試基礎：兩座 PostgreSQL（授權伺服器 auth-db、業務 API orders-db，各自帳號）與一座 Redis。
/// 第一次使用才啟動容器，測試結束時清理；未使用的測試執行不需要 Docker。
/// 連線資訊以設定鍵（等同環境變數 ConnectionStrings__AuthDb 等）提供，帳密可由環境變數覆寫，否則每次執行隨機產生。
/// </summary>
public sealed class RealStorageTestHarness : IAsyncDisposable
{
    public const string AuthDbKey = "ConnectionStrings:AuthDb";
    public const string OrdersDbKey = "ConnectionStrings:OrdersDb";
    public const string RedisKey = "ConnectionStrings:Redis";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static RealStorageTestHarness? _shared;

    private readonly PostgreSqlContainer _authDb;
    private readonly PostgreSqlContainer _ordersDb;
    private readonly RedisContainer _redis;
    private readonly int _authDbPort = SpikeEnvironment.FreeTcpPort();
    private readonly int _ordersDbPort = SpikeEnvironment.FreeTcpPort();
    private readonly int _redisPort = SpikeEnvironment.FreeTcpPort();
    private readonly string _authDbConnection;
    private readonly string _ordersDbConnection;

    private RealStorageTestHarness()
    {
        // 容器重啟後主機連接埠必須不變，因此固定綁定。
        _authDb = BuildPostgres("auth_db", "auth_svc", "AUTH_DB_PASSWORD", _authDbPort);
        _ordersDb = BuildPostgres("orders_db", "orders_svc", "ORDERS_DB_PASSWORD", _ordersDbPort);
        _redis = new RedisBuilder("redis:7-alpine").WithPortBinding(_redisPort, 6379).Build();
        _authDbConnection = PostgresConnection(_authDbPort, "auth_db", "auth_svc", "AUTH_DB_PASSWORD");
        _ordersDbConnection = PostgresConnection(_ordersDbPort, "orders_db", "orders_svc", "ORDERS_DB_PASSWORD");
    }

    public static async Task<RealStorageTestHarness> GetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (_shared is null)
            {
                var harness = new RealStorageTestHarness();
                await harness.StartAsync();
                _shared = harness;
            }

            return _shared;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ShutdownAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (_shared is not null)
            {
                await _shared.DisposeAsync();
                _shared = null;
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>供受測服務以設定／環境變數取得連線資訊；各服務只拿到自己的資料庫帳號。</summary>
    public IReadOnlyDictionary<string, string> Configuration => new Dictionary<string, string>
    {
        [AuthDbKey] = _authDbConnection,
        [OrdersDbKey] = _ordersDbConnection,
        [RedisKey] = RedisConnection,
    };

    public string AuthDbConnectionString => _authDbConnection;

    public string OrdersDbConnectionString => _ordersDbConnection;

    public string RedisConnection => $"localhost:{_redisPort},abortConnect=false,connectTimeout=2000,syncTimeout=2000,connectRetry=1";

    public async Task StopAsync(StorageService service) => await Container(service).StopAsync();

    /// <summary>重新啟動已停止的服務，並等到可接受連線為止。</summary>
    public async Task StartAsync(StorageService service)
    {
        await Container(service).StartAsync();
        await WaitUntilReadyAsync(service);
    }

    public async Task FlushRedisAsync()
    {
        await using var connection = await ConnectionMultiplexer.ConnectAsync(RedisConnection + ",allowAdmin=true");
        await connection.GetServer(connection.GetEndPoints()[0]).FlushAllDatabasesAsync();
    }

    public async Task<NpgsqlDataSource> OpenDataSourceAsync(StorageService service)
    {
        var connectionString = service switch
        {
            StorageService.AuthDb => _authDbConnection,
            StorageService.OrdersDb => _ordersDbConnection,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, "不是 PostgreSQL 服務"),
        };
        var source = NpgsqlDataSource.Create(connectionString);
        await using var connection = await source.OpenConnectionAsync();
        return source;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _authDb.DisposeAsync().AsTask(),
            _ordersDb.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask());
    }

    private async Task StartAsync()
    {
        await Task.WhenAll(_authDb.StartAsync(), _ordersDb.StartAsync(), _redis.StartAsync());
    }

    private IContainer Container(StorageService service) => service switch
    {
        StorageService.AuthDb => _authDb,
        StorageService.OrdersDb => _ordersDb,
        StorageService.Redis => _redis,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, null),
    };

    private async Task WaitUntilReadyAsync(StorageService service)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            try
            {
                if (service == StorageService.Redis)
                {
                    await using var redis = await ConnectionMultiplexer.ConnectAsync(RedisConnection);
                    await redis.GetDatabase().PingAsync();
                }
                else
                {
                    using var source = await OpenDataSourceAsync(service);
                }

                return;
            }
            catch when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(250);
            }
        }
    }

    private static PostgreSqlContainer BuildPostgres(string database, string username, string passwordVariable, int port) =>
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase(database)
            .WithUsername(username)
            .WithPassword(Password(passwordVariable))
            .WithPortBinding(port, 5432)
            .Build();

    private static string PostgresConnection(int port, string database, string username, string passwordVariable) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = port,
            Database = database,
            Username = username,
            Password = Password(passwordVariable),
            Timeout = 3,
            CommandTimeout = 3,
            Pooling = false,
        }.ConnectionString;

    private static string Password(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        // 同一次測試執行內，同一個變數需得到同一組隨機密碼。
        return GeneratedPasswords.GetOrAdd(variable, _ => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> GeneratedPasswords = new();
}
