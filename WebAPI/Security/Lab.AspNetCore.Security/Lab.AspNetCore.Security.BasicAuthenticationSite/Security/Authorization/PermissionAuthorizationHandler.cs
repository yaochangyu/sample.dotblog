using Microsoft.AspNetCore.Authorization;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
{

    public override Task HandleAsync(AuthorizationHandlerContext context)
    {
        return base.HandleAsync(context);
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        if (context.User.Identity.IsAuthenticated==false)
        {
            
            // return Task.FromResult(false);
        }

        

        // Task.FromResult(false);
    }
    
}