using Microsoft.AspNetCore.Authorization;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; init; }
}