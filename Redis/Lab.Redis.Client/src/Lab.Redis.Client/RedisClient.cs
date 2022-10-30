using StackExchange.Redis;

namespace Lab.Redis.Client;

public class RedisClient
{
    private static readonly Lazy<ConnectionMultiplexer> s_connectionLazy;
    private static string _setting;

    private ConnectionMultiplexer Instance => s_connectionLazy.Value;

    public IDatabase Database => this.Instance.GetDatabase();

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

    public static void Init(string setting)
    {
        _setting = setting;
    }
}