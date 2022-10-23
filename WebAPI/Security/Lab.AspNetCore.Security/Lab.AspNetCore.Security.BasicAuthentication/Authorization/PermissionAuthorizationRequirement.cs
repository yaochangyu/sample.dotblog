using Microsoft.AspNetCore.Authorization;

namespace Lab.AspNetCore.Security.BasicAuthentication;

public class PermissionAuthorizationRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; init; }
}