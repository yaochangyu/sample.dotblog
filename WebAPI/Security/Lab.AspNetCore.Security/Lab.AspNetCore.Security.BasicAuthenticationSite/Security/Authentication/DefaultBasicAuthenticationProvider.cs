namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class DefaultBasicAuthenticationProvider : IBasicAuthenticationProvider
{
    public Task<bool> IsValidateAsync(string user, string password, CancellationToken cancel = default)
    {
        return Task.FromResult(true);
    }
}