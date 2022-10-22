using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasic<TAuthProvider>(this AuthenticationBuilder builder,
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

    public static AuthenticationBuilder AddBasicAuthentication<TAuthProvider>(this IServiceCollection services,
        Action<BasicAuthenticationOptions> configureOptions)
        where TAuthProvider : class, IBasicAuthenticationProvider
    {
        var scheme = BasicAuthenticationDefaults.AuthenticationScheme;
        return services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = scheme;
                o.DefaultChallengeScheme = scheme;
            })
            .AddBasic<TAuthProvider>(scheme, scheme, configureOptions);
    }
}