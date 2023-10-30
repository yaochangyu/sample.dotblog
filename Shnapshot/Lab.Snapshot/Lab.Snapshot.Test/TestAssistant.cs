using Npgsql;

namespace Lab.Snapshot.Test;

public class TestAssistant
{
    public const string DbConnectionString =
        "Host=localhost;Port=5432;Database=member_integration_test;Username=postgres;Password=guest";

    public static DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

    public static string UserId { get; set; } = "@@TestUser@@";
}