namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class PermissionAuthorizationProvider : IPermissionAuthorizationProvider
{
    private readonly Dictionary<string, IEnumerable<string>> _clientPermissions =
        new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "yao", new[] { Permission.Operation.Read, Permission.Operation.Write } },
            { "jojo", new[] { Permission.Operation.Read} }
        };

    public IEnumerable<string> GetPermissions(string userId)
    {
        if (this._clientPermissions.TryGetValue(userId, out var result) == false)
        {
            result = new List<string>();
        }

        return result;
    }
}