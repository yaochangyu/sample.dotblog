namespace Lab.AspNetCore.Security.BasicAuthentication;

public interface IBasicAuthenticationProvider
{
    Task<bool> IsValidateAsync(string user, string password, CancellationToken cancel);
}