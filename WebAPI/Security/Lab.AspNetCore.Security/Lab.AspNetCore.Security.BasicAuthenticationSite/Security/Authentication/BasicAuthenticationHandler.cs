using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    private readonly IBasicAuthenticationProvider _authenticationProvider;
    private string _failReason;

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

        if (!this.Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            this._failReason = "Invalid basic authentication header";
            return AuthenticateResult.Fail(this._failReason);
        }

        if (!AuthenticationHeaderValue.TryParse(this.Request.Headers[HeaderNames.Authorization],
                out var authHeaderValue))
        {
            this._failReason = "Invalid authorization Header";
            return AuthenticateResult.Fail(this._failReason);
        }

        if (authHeaderValue.Scheme.StartsWith(schemeName, StringComparison.InvariantCultureIgnoreCase) == false)
        {
            this._failReason = "Invalid authorization scheme name";
            return AuthenticateResult.Fail("Invalid authorization scheme name");
        }

        var credentialBytes = Convert.FromBase64String(authHeaderValue.Parameter);
        var userAndPassword = Encoding.UTF8.GetString(credentialBytes);
        var credentials = userAndPassword.Split(':');
        if (credentials.Length != 2)
        {
            this._failReason = "Invalid basic authentication header";
            return AuthenticateResult.Fail(this._failReason);
        }

        var user = credentials[0];
        var password = credentials[1];

        var isValidate = await this._authenticationProvider.IsValidateAsync(user, password, CancellationToken.None);

        if (!isValidate)
        {
            this._failReason = "Invalid username or password";
            return AuthenticateResult.Fail(this._failReason);
        }

        return this.SignIn(user);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // 寫入詳細的失敗原因，排除敏感性資料 
        this.Logger.LogInformation("{FailureReason}", new
        {
            Code = "InvalidAuthentication",
            Message = this._failReason
        });

        this.Response.StatusCode = 401;
        this.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = this._failReason;
        this.Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{this.Options.Realm}\", charset=\"UTF-8\"";

        // 響應粗糙的內容，這不是標準的 Basic Authentication 失敗的回傳，僅是為了示意
        this.Response.WriteAsJsonAsync(new
        {
            Code = "InvalidAuthentication",
            Message = "Please contact your administrator"
        });
        await Task.CompletedTask;
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