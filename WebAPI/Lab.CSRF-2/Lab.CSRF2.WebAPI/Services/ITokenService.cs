namespace Lab.CSRF2.WebAPI.Services;

public interface ITokenService
{
    string GenerateToken(int maxUsageCount = 1, int expirationMinutes = 5);
    bool ValidateToken(string token);
}
