using System.Web.Http;

namespace AspNet48.WebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Filters.Add(new InjectionAttribute());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional}
                                      );
        }
    }
}