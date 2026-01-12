namespace Lab.CSRF2.WebAPI.Services;

public interface ITokenService
{
    string GenerateToken(int maxUsageCount, int expirationMinutes, string userAgent, string ipAddress);
    bool ValidateToken(string token, string userAgent, string ipAddress);
}
