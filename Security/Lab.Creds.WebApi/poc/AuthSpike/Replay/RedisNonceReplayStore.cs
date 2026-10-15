using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace AuthSpike.Replay;

/// <summary>
/// 基於 Redis 的分散式防重放儲存：
/// 1. 跨多個業務 API 執行個體一致，透過 Redis Lua 腳本達到原子判斷與登錄。
/// 2. 防重放紀元（Replay Epoch）：當 Redis 被清空或重啟後，若缺少紀元標記，初始化為當下 Redis 時間；
///    任何簽章建立時間（created）早於紀元的請求一律拒絕（BeforeReplayEpoch），避免已核准但在重啟前建立的舊簽章重放。
/// 3. Fail Closed：Redis 發生異常或連線中斷時拋出 ReplayStateUnavailableException，業務 API 回報 503。
/// </summary>
public sealed class RedisNonceReplayStore : INonceReplayStore, IAsyncDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly bool _ownsConnection;
    private readonly string _epochKey;
    private readonly string _nonceKeyPrefix;

    // Lua Script 說明：
    // KEYS[1]: epochKey (e.g. "authspike:epoch" 或依 environmentName 隔離)
    // KEYS[2]: nonceKey (e.g. "authspike:nonce:{clientId}:{hash}")
    // ARGV[1]: createdSec (簽章 created 時間之 unix epoch 秒數)
    // ARGV[2]: retainUntilMs (保留截止時間之 unix epoch ms)
    // ARGV[3]: ttlSeconds (存活秒數)
    //
    // 回傳值：
    // 1: 首次登錄成功 (Registered)
    // 0: 重放 (Replayed)
    // -1: 簽章建立時間早於防重放紀元 (BeforeReplayEpoch)
    private const string RegisterScript = """
        local redis_time = redis.call('TIME')
        local now_sec = tonumber(redis_time[1])

        local epoch_val = redis.call('GET', KEYS[1])
        local epoch_sec
        if not epoch_val then
            epoch_sec = now_sec
            redis.call('SET', KEYS[1], epoch_sec)
        else
            epoch_sec = tonumber(epoch_val)
        end

        local created_sec = tonumber(ARGV[1])
        if created_sec <= epoch_sec then
            return -1
        end

        local retain_until_ms = ARGV[2]
        local ttl_sec = tonumber(ARGV[3])
        if ttl_sec <= 0 then
            ttl_sec = 1
        end

        local set_res = redis.call('SET', KEYS[2], retain_until_ms, 'EX', ttl_sec, 'NX')
        if set_res then
            return 1
        else
            return 0
        end
    """;

    public RedisNonceReplayStore(IConnectionMultiplexer connection, string environmentName = "default", bool ownsConnection = false)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _ownsConnection = ownsConnection;
        _epochKey = $"{environmentName}:replay:epoch";
        _nonceKeyPrefix = $"{environmentName}:replay:nonce:";
    }

    public static async Task<RedisNonceReplayStore> ConnectAsync(string connectionString, string environmentName = "default")
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 2000;
        options.SyncTimeout = 2000;
        options.AsyncTimeout = 2000;
        var connection = await ConnectionMultiplexer.ConnectAsync(options);
        var store = new RedisNonceReplayStore(connection, environmentName, ownsConnection: true);
        await store.EnsureEpochInitializedAsync();
        return store;
    }

    private async Task EnsureEpochInitializedAsync()
    {
        try
        {
            var db = _connection.GetDatabase();
            var redisTime = (RedisResult[]?)await db.ExecuteAsync("TIME");
            long epochSec;
            if (redisTime is not null && redisTime.Length >= 2)
            {
                epochSec = (long)redisTime[0] - 1;
            }
            else
            {
                epochSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
            }

            await db.StringSetAsync(_epochKey, epochSec, when: When.NotExists);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException)
        {
            // 連線異常會在後續作業拋出 ReplayStateUnavailableException
        }
    }

    public async Task<ReplayRegistration> TryRegisterAsync(string clientId, string nonce, DateTimeOffset created, DateTimeOffset retainUntil)
    {
        try
        {
            var db = _connection.GetDatabase();
            var nonceKey = BuildNonceKey(clientId, nonce);
            var now = DateTimeOffset.UtcNow;
            var ttl = retainUntil > now ? retainUntil - now : TimeSpan.FromSeconds(1);
            var ttlSeconds = Math.Max(1, (int)Math.Ceiling(ttl.TotalSeconds));

            var result = await db.ScriptEvaluateAsync(
                RegisterScript,
                keys: [new RedisKey(_epochKey), new RedisKey(nonceKey)],
                values: [
                    created.ToUnixTimeSeconds(),
                    retainUntil.ToUnixTimeMilliseconds(),
                    ttlSeconds
                ]);

            if (result.IsNull)
            {
                throw new ReplayStateUnavailableException("Redis Lua 腳本回傳空值。");
            }

            var status = (long)result;
            return status switch
            {
                1 => ReplayRegistration.Registered,
                0 => ReplayRegistration.Replayed,
                -1 => ReplayRegistration.BeforeReplayEpoch,
                _ => throw new ReplayStateUnavailableException($"Redis Lua 腳本回傳非預期狀態：{status}。"),
            };
        }
        catch (ReplayStateUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException or ObjectDisposedException)
        {
            throw new ReplayStateUnavailableException("防重放狀態無法讀寫（Redis 連線或操作失敗）。", ex);
        }
    }

    public async Task<DateTimeOffset?> RetainUntilOfAsync(string clientId, string nonce)
    {
        try
        {
            var db = _connection.GetDatabase();
            var nonceKey = BuildNonceKey(clientId, nonce);
            var val = await db.StringGetAsync(nonceKey);
            if (!val.HasValue || !long.TryParse((string?)val, out var retainUntilMs))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeMilliseconds(retainUntilMs);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException or ObjectDisposedException)
        {
            throw new ReplayStateUnavailableException("防重放狀態無法讀寫（Redis 查詢失敗）。", ex);
        }
    }

    public async Task<DateTimeOffset?> GetEpochAsync()
    {
        try
        {
            var db = _connection.GetDatabase();
            var val = await db.StringGetAsync(_epochKey);
            if (!val.HasValue || !long.TryParse((string?)val, out var epochSec))
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(epochSec);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException or System.Net.Sockets.SocketException or ObjectDisposedException)
        {
            throw new ReplayStateUnavailableException("防重放狀態無法讀寫（Redis 查詢紀元失敗）。", ex);
        }
    }

    private string BuildNonceKey(string clientId, string nonce)
    {
        var raw = Encoding.UTF8.GetBytes(nonce);
        var hash = SHA256.HashData(raw);
        return $"{_nonceKeyPrefix}{clientId}:{Convert.ToHexString(hash)}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_ownsConnection)
        {
            await _connection.DisposeAsync();
        }
    }
}
