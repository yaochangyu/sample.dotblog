using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class PermissionAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(RequestDelegate next, 
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        await next(context);
    }
}