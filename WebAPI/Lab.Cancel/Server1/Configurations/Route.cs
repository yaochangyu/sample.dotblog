using System.Web.Http;
using Swagger.Net.Application;

namespace Server1
{
    public class Route
    {
        public static void Configure(HttpConfiguration httpConfig)
        {
            httpConfig.MapHttpAttributeRoutes();

            httpConfig.Routes.MapHttpRoute(
                                           "swagger_root",
                                           "",
                                           null,
                                           null,
                                           new RedirectHandler(message => message.RequestUri.ToString(), "swagger"));

            httpConfig.Routes.MapHttpRoute(
                                           "DefaultApi",
                                           "api/{controller}/{id}",
                                           new {id = RouteParameter.Optional}
                                          );
        }

    }
}