using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class JwtValidationHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            var authorization = request.Headers.Authorization;
            if (authorization != null && authorization.Scheme == "Bearer")
            {
                var token = authorization.Parameter;
                if (JwtManager.TryValidateToken(token, out var principal))
                {
                    Thread.CurrentPrincipal = principal;
                    request.GetRequestContext().Principal = principal;
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}