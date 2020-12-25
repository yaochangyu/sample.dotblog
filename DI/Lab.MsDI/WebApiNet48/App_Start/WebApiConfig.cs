using System.Collections.Generic;
using System.Web.Http;

namespace WebApiNet48
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // using Microsoft.Extension.DependencyInjection here.
            DependencyInjectionConfig.Register(config);

            // Web API configuration and services
            config.Filters.Add(new LogFilterAttribute());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
