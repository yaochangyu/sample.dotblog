using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Server
{
    public class BasicAuthorizeAttribute : AuthorizeAttribute
    {
        public override async Task OnAuthorizationAsync(HttpActionContext actionContext,
                                                        CancellationToken cancellationToken)
        {
            var authorization = actionContext.Request.Headers.Authorization;
            if (authorization != null && authorization.Scheme.ToLower() == "basic")
            {
                //base64 decode
                Tuple<string, string> userNameAndPasword = DecodeBase64(authorization.Parameter);
                string userName = userNameAndPasword.Item1;
                string password = userNameAndPasword.Item2;

                //check user at database
                IPrincipal principal = await this.AuthenticateAsync(userName, password, cancellationToken);

                if (principal != null)
                {
                    Thread.CurrentPrincipal = principal;
                    if (HttpContext.Current != null)
                    {
                        HttpContext.Current.User = principal;
                    }

                    actionContext.Request.GetRequestContext().Principal = principal;
                }
            }

            await base.OnAuthorizationAsync(actionContext, cancellationToken);
        }

        private async Task<IPrincipal> AuthenticateAsync(string userName, string password,
                                                         CancellationToken cancellationToken)
        {
            //Check User
            if (!await SecurityManager.CheckUserAsync(userName, password, cancellationToken))
            {
                return null;
            }

            // Create a ClaimsIdentity with all the claims for this user.
            cancellationToken
                .ThrowIfCancellationRequested(); // Unfortunately, IClaimsIdenityFactory doesn't support CancellationTokens.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName)

                // Add more claims if needed: Roles, ...
            };
            var identity = new ClaimsIdentity(claims, "Basic");
            return new ClaimsPrincipal(identity);
        }

        private static Tuple<string, string> DecodeBase64(string authorizationParameter)
        {
            byte[] credentialBytes;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return null;
            }

            // The currently approved HTTP 1.1 specification says characters here are ISO-8859-1.
            // However, the current draft updated specification for HTTP 1.1 indicates this encoding is infrequently
            // used in practice and defines behavior only for ASCII.
            Encoding encoding = Encoding.ASCII;

            // Make a writable copy of the encoding to enable setting a decoder fallback.
            encoding = (Encoding) encoding.Clone();

            // Fail on invalid bytes rather than silently replacing and continuing.
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            string decodedCredentials;

            try
            {
                decodedCredentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(decodedCredentials))
            {
                return null;
            }

            int colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1)
            {
                return null;
            }

            string userName = decodedCredentials.Substring(0, colonIndex);
            string password = decodedCredentials.Substring(colonIndex + 1);
            return new Tuple<string, string>(userName, password);
        }
    }
}