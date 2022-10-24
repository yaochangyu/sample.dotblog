using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using StackExchange.Redis;

namespace TestProject1;

public class RedisClient
{
    private static string _setting;

    static RedisClient()
    {
        s_connectionLazy = new Lazy<ConnectionMultiplexer>(() =>
        {
            if (string.IsNullOrWhiteSpace(_setting))
            {
                return ConnectionMultiplexer.Connect("localhost");
            }

            return ConnectionMultiplexer.Connect(_setting);
        });
    }

    private static readonly Lazy<ConnectionMultiplexer> s_connectionLazy;

    public ConnectionMultiplexer Instance => s_connectionLazy.Value;

    public IDatabase Database => this.Instance.GetDatabase();

    public static void Init(string setting)
    {
        _setting = setting;
    }

    public T Get<T>(string key)
    {
        if (Exists(key))
        {
            return Deserialize<T>(this.Database.StringGet(key));
        }

        throw new Exception();
    }
    public T Get2<T>(string key)
    {
        if (Exists(key))
        {
            return JsonSerializer.Deserialize<T>(this.Database.StringGet(key));
        }

        throw new Exception();
    }
    public bool Exists(string key)
    {
        return this.Database.KeyExists(key); //可直接調用
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = default(TimeSpan?), When when = When.Always,
        CommandFlags flags = CommandFlags.None)
    {
        this.Database.StringSet(key, Serialize(value), expiry, when, flags);
    }
    public void Set2<T>(string key, T value, TimeSpan? expiry = default(TimeSpan?), When when = When.Always,
        CommandFlags flags = CommandFlags.None)
    {
        this.Database.StringSet(key,JsonSerializer.Serialize(value) , expiry, when, flags);
    }

    private static byte[] Serialize(object instance)
    {
        if (instance == null)
        {
            return null;
        }

        var formatter = new BinaryFormatter();
        using var outputStream = new MemoryStream();
        formatter.Serialize(outputStream, instance);
        return outputStream.ToArray();
    }

    private static T Deserialize<T>(byte[] srcStream)
    {
        if (srcStream == null)
        {
            return default(T);
        }

        var formatter = new BinaryFormatter();
        using var outputStream = new MemoryStream(srcStream);
        return (T)formatter.Deserialize(outputStream);
    }
}

public class RedisConnection2
{
    private static readonly Lazy<RedisConnection2> s_redisConnectionLazy = new(() => new RedisConnection2());

    private static string _setting;

    public readonly ConnectionMultiplexer ConnectionMultiplexer;

    public static RedisConnection2 Instance => s_redisConnectionLazy.Value;

    private RedisConnection2()
    {
        if (string.IsNullOrWhiteSpace(_setting))
        {
            _setting = "localhost";
        }

        this.ConnectionMultiplexer = ConnectionMultiplexer.Connect(_setting);
    }

    public static void Init(string setting)
    {
        _setting = setting;
    }
}