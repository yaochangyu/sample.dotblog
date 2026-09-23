using System.Security.Cryptography;

namespace Lab.Signature.WebApi.Services;

public class ApiKeyGenerator : IApiKeyGenerator
{
    private const int ApiKeyRandomBytes = 16;
    private const int SecretRandomBytes = 32;

    public string GenerateApiKey() =>
        "key-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(ApiKeyRandomBytes));

    public string GenerateSecret() =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(SecretRandomBytes));
}
