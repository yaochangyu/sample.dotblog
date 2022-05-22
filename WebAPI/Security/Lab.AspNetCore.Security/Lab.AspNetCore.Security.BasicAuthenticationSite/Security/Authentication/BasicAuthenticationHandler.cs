using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    private const string AuthorizationHeaderName = "Authorization";
    private readonly IBasicAuthenticationProvider _authenticationProvider;

    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IBasicAuthenticationProvider authenticationProvider)
        : base(options, logger, encoder, clock)
    {
        this._authenticationProvider = authenticationProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var schemeName = this.Scheme.Name; //由外部注入
        var endpoint = this.Context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            return AuthenticateResult.NoResult();
        }

        if (!this.Request.Headers.ContainsKey(AuthorizationHeaderName))
        {
            return AuthenticateResult.Fail("Invalid basic authentication header");
        }

        if (!AuthenticationHeaderValue.TryParse(this.Request.Headers[AuthorizationHeaderName],
                out var authHeaderValue))
        {
            return AuthenticateResult.Fail("Invalid authorization Header");
        }

        if (authHeaderValue.Scheme.StartsWith(schemeName, StringComparison.InvariantCultureIgnoreCase) == false)
        {
            return AuthenticateResult.Fail("Invalid authorization scheme name");
        }

        var credentialBytes = Convert.FromBase64String(authHeaderValue.Parameter);
        var userAndPassword = Encoding.UTF8.GetString(credentialBytes);
        var credentials = userAndPassword.Split(':');
        if (credentials.Length != 2)
        {
            return AuthenticateResult.Fail("Invalid basic authentication header");
        }

        var user = credentials[0];
        var password = credentials[1];

        var isValidate = await this._authenticationProvider.IsValidateAsync(user, password, CancellationToken.None);

        if (!isValidate)
        {
            return AuthenticateResult.Fail("Invalid username or password");
        }

        return this.SignIn(user);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        this.Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{this.Options.Realm}\", charset=\"UTF-8\"";
        await base.HandleChallengeAsync(properties);
    }

    private AuthenticateResult SignIn(string user)
    {
        var schemeName = this.Scheme.Name;
        var claims = new[] { new Claim(ClaimTypes.Name, user) };
        var identity = new ClaimsIdentity(claims, schemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, schemeName);
        return AuthenticateResult.Success(ticket);
    }
}