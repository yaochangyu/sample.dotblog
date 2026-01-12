namespace Lab.CSRF2.WebAPI.Providers;

public interface ITokenProvider
{
    string GenerateToken(int maxUsageCount, int expirationMinutes, string userAgent, string ipAddress);
    bool ValidateToken(string token, string userAgent, string ipAddress);
}
