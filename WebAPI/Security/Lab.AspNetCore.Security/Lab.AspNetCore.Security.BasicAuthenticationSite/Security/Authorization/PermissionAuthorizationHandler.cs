using Microsoft.AspNetCore.Authorization;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    private readonly IPermissionAuthorizationProvider _authorizationProvider;

    public PermissionAuthorizationHandler(IPermissionAuthorizationProvider authorizationProvider)
    {
        this._authorizationProvider = authorizationProvider;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        if (context.User.Identity.IsAuthenticated == false)
        {
            context.Fail(new AuthorizationFailureReason(this, $"目前請求沒有通過驗證"));
            return;
        }

        var userId = context.User.Identity.Name;
        var permissions = this._authorizationProvider.GetPermissions(userId);
        if (permissions.Any(p => p.StartsWith(requirement.PolicyName, StringComparison.InvariantCultureIgnoreCase)) ==
            false)
        {
            context.Fail(new AuthorizationFailureReason(this, $"用戶 '{userId}'，沒有授權 '{requirement.PolicyName}'"));
        }

        if (context.HasFailed == false)
        {
            context.Succeed(requirement);
        }
    }
}