using System.Web.Http;
using Lab.Compress.ViaDecompressHandler.Handlers;

namespace Lab.Compress.ViaDecompressHandler
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.MessageHandlers.Add(new DecompressHandler());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{action}/{id}",
                                       new {id = RouteParameter.Optional}
                                      );
        }
    }
}