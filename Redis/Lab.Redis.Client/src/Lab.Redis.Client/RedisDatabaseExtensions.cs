using System.Text.Json;
using StackExchange.Redis;

namespace Lab.Redis.Client;

public static class RedisDatabaseExtensions
{
    public static bool IsExist(this IDatabase db, string key)
    {
        return db.KeyExists(key);
    }

    public static void Set<T>(this IDatabase db, string key, T value,
        TimeSpan? expiry = default,
        When when = When.Always,
        CommandFlags flags = CommandFlags.None,
        JsonSerializerOptions options = default)
    {
        db.StringSet(key, Serialize(value, options), expiry, when, flags);
    }

    private static string Serialize<T>(T value, JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(value, options);
    }

    public static T Get<T>(this IDatabase db, string key, JsonSerializerOptions options = default)
    {
        if (db.IsExist(key))
        {
            return Deserialize<T>(db.StringGet(key), options);
        }

        return default;
    }

    private static T? Deserialize<T>(RedisValue value, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(value, options);
    }
}