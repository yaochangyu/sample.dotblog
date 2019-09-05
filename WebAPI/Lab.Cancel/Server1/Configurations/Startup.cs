using System.Web.Http;
using Owin;

namespace Server1
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var httpConfig = new HttpConfiguration();

            Route.Configure(httpConfig);
            Swagger.Configure(httpConfig);

            // Use the extension method provided by the WebApi.Owin library:
            app.UseWebApi(httpConfig);
        }
    }
}