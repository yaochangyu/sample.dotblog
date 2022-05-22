using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    private const string AuthorizationHeaderName = "Authorization";
    private const string BasicSchemeName = "Basic";
    private readonly IBasicAuthenticationProvider _authenticationProvider;

    public BasicAuthenticationHandler(
        IOptionsMonitor<BasicAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IBasicAuthenticationProvider authenticationService)
        : base(options, logger, encoder, clock)
    {
        this._authenticationProvider = authenticationService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!this.Request.Headers.ContainsKey(AuthorizationHeaderName))
        {
            // return AuthenticateResult.Fail("Invalid Basic authentication header");
            //Authorization header not in request
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse(this.Request.Headers[AuthorizationHeaderName],
                out var headerValue))
        {
            //Invalid Authorization header
            return AuthenticateResult.NoResult();
        }

        if (headerValue.Scheme.StartsWith(BasicSchemeName, StringComparison.InvariantCultureIgnoreCase) == false)
        {
            return AuthenticateResult.NoResult();
        }

        if (!BasicSchemeName.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            //Not Basic authentication header
            return AuthenticateResult.NoResult();
        }

        var headerValueBytes = Convert.FromBase64String(headerValue.Parameter);
        var userAndPassword = Encoding.UTF8.GetString(headerValueBytes);
        var parts = userAndPassword.Split(':');
        if (parts.Length != 2)
        {
            return AuthenticateResult.Fail("Invalid Basic authentication header");
        }

        var user = parts[0];
        var password = parts[1];

        var isValidUser = await this._authenticationProvider.IsValidUserAsync(user, password);

        if (!isValidUser)
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
        var claims = new[] { new Claim(ClaimTypes.Name, user) };
        var identity = new ClaimsIdentity(claims, this.Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}