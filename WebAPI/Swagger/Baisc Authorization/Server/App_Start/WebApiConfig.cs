using System.Web.Http;

namespace Server
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            //config.Filters.Add(new AuthorizeAttribute());
            config.Filters.Add(new BasicAuthorizeAttribute());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new { id = RouteParameter.Optional }
                                      );
        }
    }
}