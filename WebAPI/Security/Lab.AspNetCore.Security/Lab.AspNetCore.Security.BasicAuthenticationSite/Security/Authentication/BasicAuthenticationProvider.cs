namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class BasicAuthenticationProvider : IBasicAuthenticationProvider
{
    public Task<bool> IsValidUserAsync(string user, string password)
    {
        return Task.FromResult(true);
    }
}