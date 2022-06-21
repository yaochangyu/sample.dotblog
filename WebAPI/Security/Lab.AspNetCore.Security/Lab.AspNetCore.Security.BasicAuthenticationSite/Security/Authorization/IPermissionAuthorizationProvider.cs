namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public interface IPermissionAuthorizationProvider
{
   IEnumerable<string> GetPermissions(string userId);
}