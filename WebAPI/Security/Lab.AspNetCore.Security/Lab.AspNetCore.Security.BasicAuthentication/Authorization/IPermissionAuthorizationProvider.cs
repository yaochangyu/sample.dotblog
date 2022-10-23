namespace Lab.AspNetCore.Security.BasicAuthentication;

public interface IPermissionAuthorizationProvider
{
   IEnumerable<string> GetPermissions(string userId);
}