using System.Collections.Concurrent;
using StackExchange.Redis;

namespace TestProject1;

public class RedisConnection
{
    private static ConcurrentDictionary<string, ConnectionMultiplexer> s_connections = new();

    public IDatabase Connect(string setting = "localhost")
    {
        var connMultiplexer = ConnectionMultiplexer.Connect(setting);
        s_connections.TryAdd(setting, connMultiplexer);
        return connMultiplexer.GetDatabase();
    }

    public IDatabase GetDatabase(string setting = "localhost")
    {
        if (s_connections.TryGetValue(setting, out var connMultiplexer))
        {
            return connMultiplexer.GetDatabase();
        }

        return null;
    }
}