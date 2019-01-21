using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Filters
{
    public class IdentityBasicAuthenticationAttribute : BasicAuthenticationAttribute
    {
        protected override async Task<IPrincipal> AuthenticateAsync(string userName, string password,
                                                                    CancellationToken cancellationToken)
        {
            //Check User
            if (this.CheckUser(userName, password) == false)
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

        private bool CheckUser(string userName, string password)
        {
            return true;
        }
    }
}