using Microsoft.AspNetCore.Authentication;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasic(this AuthenticationBuilder builder,
        Action<BasicAuthenticationOptions> configureOptions)
    {
        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            BasicAuthenticationDefaults.AuthenticationScheme,
            BasicAuthenticationDefaults.AuthenticationScheme, configureOptions);
    }

    public static AuthenticationBuilder AddBasicAuthentication(this IServiceCollection services,
        Action<BasicAuthenticationOptions> configureOptions)
    {
        return services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = BasicAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = BasicAuthenticationDefaults.AuthenticationScheme;
            })
            .AddBasic(configureOptions);
    }
}