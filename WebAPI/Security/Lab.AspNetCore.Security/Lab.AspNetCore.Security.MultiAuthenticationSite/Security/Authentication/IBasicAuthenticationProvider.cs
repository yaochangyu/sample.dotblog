namespace Lab.AspNetCore.Security.MultiAuthenticationSite.Security.Authentication;

public interface IBasicAuthenticationProvider
{
    Task<bool> IsValidateAsync(string user, string password, CancellationToken cancel);
}