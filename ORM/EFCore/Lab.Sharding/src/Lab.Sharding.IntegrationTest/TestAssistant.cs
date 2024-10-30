using Lab.Sharding.WebAPI;

namespace JobBank1111.Job.WebAPI.IntegrationTest;

public enum MyEnum
{
    MyProperty = 0,
}

class TestAssistant
{
    public static void SetEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("JOB1111_ENVIRONMENT", "QA");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable(nameof(EXTERNAL_API), "http://localhost:5000/api");
    }

    public static void SetDbConnectionEnvironmentVariable(string connectionString)
    {
        Environment.SetEnvironmentVariable(nameof(SYS_DATABASE_CONNECTION_STRING1), connectionString);
    }

    public static void SetRedisConnectionEnvironmentVariable(string url)
    {
        Environment.SetEnvironmentVariable(nameof(SYS_REDIS_URL), url);
    }

    public static void SetExternalConnectionEnvironmentVariable(string url)
    {
        Environment.SetEnvironmentVariable(nameof(EXTERNAL_API), url);
    }

    public static DateTime ToUtc(string time)
    {
        var tempTime = DateTimeOffset.Parse(time);
        var utcTime = new DateTimeOffset(tempTime.DateTime, TimeSpan.Zero).UtcDateTime;
        return utcTime;
    }
}