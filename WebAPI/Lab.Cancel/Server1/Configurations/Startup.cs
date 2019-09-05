using System.Web.Http;
using Owin;
using Swagger.Net.Application;

namespace Server1
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = this.ConfigureRoute();
            SwaggerConfig.Register(config);

            // Use the extension method provided by the WebApi.Owin library:
            app.UseWebApi(config);
        }

        private HttpConfiguration ConfigureRoute()
        {
            var config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                                       "swagger_root",
                                       "",
                                       null,
                                       null,
                                       new RedirectHandler(message => message.RequestUri.ToString(), "swagger"));

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional});
            return config;
        }
    }
}