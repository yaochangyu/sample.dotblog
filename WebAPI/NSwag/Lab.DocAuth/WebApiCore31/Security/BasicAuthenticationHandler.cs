using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApiCore31.Security
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IBasicAuthenticationProvider _authenticationProvider;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory                               logger,
            UrlEncoder                                   encoder,
            ISystemClock                                 clock,
            IBasicAuthenticationProvider                 authenticationProvider)
            : base(options, logger, encoder, clock)
        {
            this._authenticationProvider = authenticationProvider;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!this.Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Missing Authorization Header");
            }

            AuthenticationHeaderValue.TryParse(this.Request.Headers["Authorization"], out var authenticationHeader);
            if (authenticationHeader == null)
            {
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }

            var credentialBytes = Convert.FromBase64String(authenticationHeader.Parameter);
            var credentials     = Encoding.UTF8.GetString(credentialBytes).Split(new[] {':'}, 2);
            var userId          = credentials[0];
            var password        = credentials[1];
            var isValid         = await this._authenticationProvider.Authenticate(userId,password);
            if (!isValid)
            {
                return AuthenticateResult.Fail("Invalid Username or Password");
            }

            //建立Claim，若需要更多資訊可以從資料庫拿
            var claims = new[]
            {
                //new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, userId)
            };
            var identity  = new ClaimsIdentity(claims, this.Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket    = new AuthenticationTicket(principal, this.Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}