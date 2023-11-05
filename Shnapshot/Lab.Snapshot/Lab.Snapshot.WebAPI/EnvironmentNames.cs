namespace Lab.Snapshot.WebAPI;

public class EnvironmentNames
{
    public const string DbConnectionString = "DB_CONNECTION_STRING";

    public static Dictionary<string, string> Values = new()
    {
        {
            DbConnectionString,
            "Host=localhost;Port=5432;Database=member_integration_test;Username=postgres;Password=guest"
        }
    };
    

}