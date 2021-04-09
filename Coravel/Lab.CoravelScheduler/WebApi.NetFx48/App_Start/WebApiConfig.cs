using System.Web.Http;
using Swagger.Net.Application;

namespace WebApi.NetFx48
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional}
                                      );
            config.Routes.MapHttpRoute(
                                       "swagger_root",
                                       "",
                                       null,
                                       null,
                                       new RedirectHandler(message => message.RequestUri.ToString(), "swagger"));
        }
    }
}