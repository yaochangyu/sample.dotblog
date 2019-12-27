using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.Owin;

namespace Lab.HangfireApp
{
    public class DashboardBasicAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // In case you need an OWIN context, use the next line, `OwinContext` class
            // is the part of the `Microsoft.Owin` package.

            var owinContext = new OwinContext(context.GetOwinEnvironment());
            if (owinContext.Request.Scheme != "https")
            {
                string redirectUri = new UriBuilder("https", owinContext.Request.Host.ToString(), 443, context.Request.Path).ToString();

                owinContext.Response.StatusCode = 301;
                owinContext.Response.Redirect(redirectUri);
                return false;
            }
            if (owinContext.Request.IsSecure == false)
            {
                owinContext.Response.Write("Secure connection is required to access Hangfire Dashboard.");
                return false;
            }
            var user        = owinContext.Authentication.User;
            if (user != null)
            {
                if (user.Identity.IsAuthenticated)
                {
                    return true;
                }
            }

            // Allow all authenticated users to see the Dashboard (potentially dangerous).
            string header = owinContext.Request.Headers["Authorization"];
            if (!string.IsNullOrWhiteSpace(header))
            {
                var auHeader = AuthenticationHeaderValue.Parse(header);
                if ("Basic".Equals(auHeader.Scheme, StringComparison.InvariantCultureIgnoreCase))
                {
                    var split = Encoding.UTF8
                                        .GetString(Convert.FromBase64String(auHeader.Parameter))
                                        .Split(':');
                    if (split.Length == 2)
                    {
                        string userId   = split[0];
                        string password = split[1];
                        if (string.Compare(userId,   "yao",         true) == 0 &&
                            string.Compare(password, "pass@w0rd1~", true) == 0)
                        {
                            var claims = new List<Claim>();
                            claims.Add(new Claim(ClaimTypes.Name, "yao"));
                            claims.Add(new Claim(ClaimTypes.Role, "admin"));
                            var identity = new ClaimsIdentity(claims, "HangfireLogin");
                            owinContext.Authentication.SignIn(identity);
                            return true;
                        }
                    }
                }
            }

            return this.Challenge(owinContext);
        }

        private bool Challenge(OwinContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
            context.Response.Write("Authenticatoin is required.");
            return false;
        }
    }
}