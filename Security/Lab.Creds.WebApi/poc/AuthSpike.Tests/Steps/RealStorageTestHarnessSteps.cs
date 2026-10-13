using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Npgsql;
using Reqnroll;
using StackExchange.Redis;

namespace AuthSpike.Tests.Steps;

[Binding]
public sealed class RealStorageTestHarnessSteps
{
    private RealStorageTestHarness _harness = null!;
    private StorageService _service;
    private ServiceInstance _a = null!;
    private ServiceInstance _b = null!;
    private Exception? _failure;

    [Given("同一個服務的兩個執行個體連到 {string}")]
    public async Task GivenTwoInstances(string service)
    {
        _harness = await RealStorageTestHarness.GetAsync();
        _service = ParseService(service);
        _a = await ServiceInstance.CreateAsync(_harness, _service);
        _b = await ServiceInstance.CreateAsync(_harness, _service);
    }

    [When("執行個體 A 寫入資料 {string}")]
    public Task WhenAWrites(string key) => _a.WriteAsync(key);

    [Then("執行個體 B 讀得到資料 {string}")]
    public async Task ThenBReads(string key) => (await _b.ReadAsync(key)).Should().BeTrue();

    [Then("執行個體 B 讀不到資料 {string}")]
    public async Task ThenBCannotRead(string key) => (await _b.ReadAsync(key)).Should().BeFalse();

    [Then("{string} 讀不到資料 {string}")]
    public async Task ThenOtherServiceCannotRead(string service, string key)
    {
        await using var other = await ServiceInstance.CreateAsync(_harness, ParseService(service));
        (await other.ReadAsync(key)).Should().BeFalse();
    }

    [When("停止 {string}")]
    public async Task WhenStop(string service)
    {
        _harness ??= await RealStorageTestHarness.GetAsync();
        await _harness.StopAsync(ParseService(service));
    }

    [When("重新啟動 {string}")]
    public async Task WhenRestart(string service)
    {
        _harness ??= await RealStorageTestHarness.GetAsync();
        await _harness.StartAsync(ParseService(service));
    }

    [When("清空 Redis")]
    public async Task WhenFlushRedis()
    {
        _harness ??= await RealStorageTestHarness.GetAsync();
        await _harness.FlushRedisAsync();
    }

    [Then("執行個體 A 的操作得到明確失敗")]
    public async Task ThenAFails()
    {
        _failure = await Xunit.Record.ExceptionAsync(() => _a.ReadAsync("any"));
        _failure.Should().NotBeNull();
        _failure.Should().Match(ex => ex is NpgsqlException || ex is RedisException || ex is TimeoutException || ex is System.Net.Sockets.SocketException);
    }

    [Then("執行個體 B 的操作成功")]
    public async Task ThenBSucceeds()
    {
        await using var fresh = await ServiceInstance.CreateAsync(_harness, _service);
        await fresh.ReadAsync("any");
    }

    [AfterScenario]
    public async Task EnsureServicesRunning()
    {
        if (_harness is null)
        {
            return;
        }

        var exceptions = new List<Exception>();
        foreach (var service in Enum.GetValues<StorageService>())
        {
            try
            {
                await _harness.StartAsync(service);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (_a is not null)
        {
            await _a.DisposeAsync();
        }

        if (_b is not null)
        {
            await _b.DisposeAsync();
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException("情境結束後復原儲存服務失敗", exceptions);
        }
    }

    private static StorageService ParseService(string name) => name switch
    {
        "auth-db" => StorageService.AuthDb,
        "orders-db" => StorageService.OrdersDb,
        "redis" => StorageService.Redis,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "未知的儲存服務"),
    };

    /// <summary>模擬同一個服務的一個執行個體：各自持有獨立連線設定，指向同一組後端儲存。</summary>
    private sealed class ServiceInstance : IAsyncDisposable
    {
        private readonly StorageService _service;
        private readonly string _connectionString;
        private readonly string _redisConnection;

        private ServiceInstance(StorageService service, string connectionString, string redisConnection)
        {
            _service = service;
            _connectionString = connectionString;
            _redisConnection = redisConnection;
        }

        public static async Task<ServiceInstance> CreateAsync(RealStorageTestHarness harness, StorageService service)
        {
            var instance = new ServiceInstance(
                service,
                service == StorageService.OrdersDb ? harness.OrdersDbConnectionString : harness.AuthDbConnectionString,
                harness.RedisConnection);
            if (service != StorageService.Redis)
            {
                await using var connection = new NpgsqlConnection(instance._connectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("create table if not exists harness_items (id text primary key)", connection);
                await command.ExecuteNonQueryAsync();
            }

            return instance;
        }

        public async Task WriteAsync(string key)
        {
            if (_service == StorageService.Redis)
            {
                await using var redis = await ConnectionMultiplexer.ConnectAsync(_redisConnection);
                await redis.GetDatabase().StringSetAsync(key, "1");
                return;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("insert into harness_items (id) values (@id) on conflict do nothing", connection);
            command.Parameters.AddWithValue("id", key);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> ReadAsync(string key)
        {
            if (_service == StorageService.Redis)
            {
                await using var redis = await ConnectionMultiplexer.ConnectAsync(_redisConnection);
                return await redis.GetDatabase().KeyExistsAsync(key);
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("select count(*) from harness_items where id = @id", connection);
            command.Parameters.AddWithValue("id", key);
            return (long)(await command.ExecuteScalarAsync())! > 0;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
