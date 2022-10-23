using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class ApiKey : IApiKey
{
    public string Key { get; init; }

    public string OwnerName { get; init; }

    public IReadOnlyCollection<Claim> Claims { get; init; }
}

public class ApiKeyProvider : IApiKeyProvider
{
    private readonly ILogger<IApiKeyProvider> _logger;

    public ApiKeyProvider(ILogger<IApiKeyProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IApiKey> ProvideAsync(string key)
    {
        var result = new ApiKey
        {
            Key = "9527",
            OwnerName = "yao",
            Claims = new List<Claim>()
            {
                new(ClaimTypes.Name, "yao")
            }
        };
        return result;
    }
}