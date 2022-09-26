using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authorization;

public class PermissionAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly ILogger<PermissionAuthorizationMiddlewareResultHandler> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public PermissionAuthorizationMiddlewareResultHandler(
        ILogger<PermissionAuthorizationMiddlewareResultHandler> logger,
        JsonSerializerOptions jsonSerializerOptions)
    {
        this._logger = logger;
        this._jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var permissionAuthorizationRequirements = policy.Requirements.OfType<PermissionAuthorizationRequirement>();
        
        if (authorizeResult.Forbidden
            && permissionAuthorizationRequirements.Any())
        {
            context.Response.StatusCode = 403;
            this._logger.LogInformation("{AuthorizationFailureResults}", new
            {
                ErrorCode = "Invalid Authorization",
                ErrorMessages = authorizeResult.AuthorizationFailure.FailureReasons
            });

            // 回傳前端模糊訊息
            await context.Response.WriteAsJsonAsync(new
            {
                ErrorCode = "Invalid Authorization",
                ErrorMessages = new[] { "Please contact your administrator" }

                // ErrorMessages = authorizeResult.AuthorizationFailure.FailureReasons
            }, this._jsonSerializerOptions);
            return;
        }

        await this._defaultHandler.HandleAsync(next, context, policy, authorizeResult);

        // await next.Invoke(context);
    }
}