using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public static class BasicAuthenticationExtensions
{
    public static AuthenticationBuilder AddBasicAuthentication<TAuthService>(this IServiceCollection services)
        where TAuthService : class, IBasicAuthenticationProvider
        => services.AddAuthentication(o => o.DefaultScheme = BasicAuthenticationDefaults.AuthenticationScheme)
            .AddBasic<TAuthService>();

    public static AuthenticationBuilder AddBasic<TAuthService>(this AuthenticationBuilder builder)
        where TAuthService : class, IBasicAuthenticationProvider =>
        AddBasic<TAuthService>(builder, BasicAuthenticationDefaults.AuthenticationScheme, _ => { });

    public static AuthenticationBuilder AddBasic<TAuthService>(this AuthenticationBuilder builder,
        string authenticationScheme)
        where TAuthService : class, IBasicAuthenticationProvider =>
        AddBasic<TAuthService>(builder, authenticationScheme, _ => { });

    public static AuthenticationBuilder AddBasic<TAuthService>(this AuthenticationBuilder builder,
        Action<BasicAuthenticationOptions> configureOptions)
        where TAuthService : class, IBasicAuthenticationProvider =>
        AddBasic<TAuthService>(builder, BasicAuthenticationDefaults.AuthenticationScheme, configureOptions);

    public static AuthenticationBuilder AddBasic<TAuthService>(this AuthenticationBuilder builder,
        string authenticationScheme, Action<BasicAuthenticationOptions> configureOptions)
        where TAuthService : class, IBasicAuthenticationProvider
    {
        builder.Services
            .AddSingleton<IPostConfigureOptions<BasicAuthenticationOptions>, BasicAuthenticationPostConfigureOptions>();
        builder.Services.AddSingleton<IBasicAuthenticationProvider, TAuthService>();

        return builder.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
            authenticationScheme, configureOptions);
    }
}