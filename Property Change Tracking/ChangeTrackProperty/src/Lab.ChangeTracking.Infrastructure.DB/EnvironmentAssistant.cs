namespace Lab.ChangeTracking.Infrastructure.DB;

public class EnvironmentAssistant
{
    public static string GetEnvironmentVariable(string key)
    {
        var result = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new Exception($"the key '{key}' not exist in environment variable");
        }

        return result;
    }
}