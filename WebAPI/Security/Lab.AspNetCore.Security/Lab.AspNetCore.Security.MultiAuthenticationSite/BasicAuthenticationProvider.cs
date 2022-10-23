namespace Lab.AspNetCore.Security.BasicAuthentication;

public class BasicAuthenticationProvider : IBasicAuthenticationProvider
{
    private readonly Dictionary<string, string> _clientIdentities = new(StringComparer.InvariantCultureIgnoreCase)
    {
        { "yao", "9527" }
    };

    public Task<bool> IsValidateAsync(string user, string password, CancellationToken cancel = default)
    {
        if (this._clientIdentities.TryGetValue(user, out var secret) == false)
        {
            return Task.FromResult(false);
        }

        if (password != secret)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}