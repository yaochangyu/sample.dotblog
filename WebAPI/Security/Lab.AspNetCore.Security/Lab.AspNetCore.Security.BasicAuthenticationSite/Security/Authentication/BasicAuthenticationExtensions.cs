using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasic<TAuthService>(this AuthenticationBuilder builder,
        string authenticationScheme,
        string displayName,
        Action<BasicAuthenticationOptions> configureOptions)
        where TAuthService : class, IBasicAuthenticationProvider
    {
        builder.Services
            .AddSingleton<IPostConfigureOptions<BasicAuthenticationOptions>, BasicAuthenticationPostConfigureOptions>();
        builder.Services.AddSingleton<IBasicAuthenticationProvider, TAuthService>();

        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            authenticationScheme,
            displayName,
            configureOptions);
    }

    public static AuthenticationBuilder AddBasicAuthentication<TAuthService>(this IServiceCollection services,
        Action<BasicAuthenticationOptions> configureOptions)
        where TAuthService : class, IBasicAuthenticationProvider
    {
        var scheme = BasicAuthenticationDefaults.AuthenticationScheme;
        return services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = scheme;
                o.DefaultChallengeScheme = scheme;
            })
            .AddBasic<TAuthService>(scheme, scheme, configureOptions);
    }
}