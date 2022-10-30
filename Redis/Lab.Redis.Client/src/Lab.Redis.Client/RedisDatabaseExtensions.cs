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
        CommandFlags flags = CommandFlags.None)
    {
        db.StringSet(key, JsonSerializer.Serialize(value), expiry, when, flags);
    }

    public static T Get<T>(this IDatabase db, string key)
    {
        if (db.IsExist(key))
        {
            return JsonSerializer.Deserialize<T>(db.StringGet(key));
        }

        return default;
    }
}