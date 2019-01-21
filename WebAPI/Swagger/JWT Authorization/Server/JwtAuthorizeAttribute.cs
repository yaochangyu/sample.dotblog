using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Security;

namespace Server
{
    public class JwtAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var authorization = actionContext.Request.Headers.Authorization;
            if (authorization != null && authorization.Scheme.ToLower() == "bearer")
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