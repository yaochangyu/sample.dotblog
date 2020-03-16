using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace AuthServer
{
    public class JwtAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var authorization = actionContext.Request.Headers.Authorization;
            if (authorization != null && authorization.Scheme == "Bearer")
            {
                var token = authorization.Parameter;
                if (JwtManager.TryValidateToken(token, out var principal))
                {
                    Thread.CurrentPrincipal = principal;
                    if (HttpContext.Current != null)
                    {
                        HttpContext.Current.User = principal;
                    }

                    actionContext.Request.GetRequestContext().Principal = principal;
                }
            }

            return base.IsAuthorized(actionContext);
        }
    }
}