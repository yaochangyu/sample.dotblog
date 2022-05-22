namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public interface IBasicAuthenticationProvider
{
    Task<bool> IsValidUserAsync(string user, string password);
}