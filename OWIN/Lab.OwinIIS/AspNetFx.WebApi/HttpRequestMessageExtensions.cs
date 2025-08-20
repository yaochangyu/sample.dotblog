using System.Net.Http;
using System.Web;
using Microsoft.Owin;

namespace AspNetFx.WebApi
{
    public static class HttpRequestMessageExtensions
    {
        public static IHttpContextProvider GetHttpContextProvider(this HttpRequestMessage request)
        {
            IHttpContextProvider provider;
            if (request.Properties.TryGetValue("MS_OwinContext", out object property))
            {
                var context = property as OwinContext;
                provider = new OWinHttpContextProvider(context);
            }
            else
            {
                var context = new HttpContextWrapper(HttpContext.Current);
                provider = new HttpContextProvider(context);
            }
            return provider;
        }
    }
}
