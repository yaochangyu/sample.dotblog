using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lab.AspNetCore.Security.BasicAuthentication;

public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasicAuthentication<TAuthProvider>(this AuthenticationBuilder builder,
        string authenticationScheme,
        string displayName,
        Action<BasicAuthenticationOptions> configureOptions)
        where TAuthProvider : class, IBasicAuthenticationProvider
    {
        builder.Services
            .AddSingleton<IPostConfigureOptions<BasicAuthenticationOptions>, BasicAuthenticationPostConfigureOptions>();
        builder.Services.AddSingleton<IBasicAuthenticationProvider, TAuthProvider>();

        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            authenticationScheme,
            displayName,
            configureOptions);
    }
    public static AuthenticationBuilder AddBasicAuthentication<TAuthProvider>(this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<BasicAuthenticationOptions> configureOptions)
        where TAuthProvider : class, IBasicAuthenticationProvider
    {
        return AddBasicAuthentication<TAuthProvider>(builder, authenticationScheme, null, configureOptions);
    }

}