namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public interface IBasicAuthenticationProvider
{
    Task<bool> IsValidateAsync(string user, string password, CancellationToken cancel);
}