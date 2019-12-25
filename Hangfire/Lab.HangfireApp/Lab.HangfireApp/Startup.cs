using System.Web.Http;
using Owin;
using Swagger.Net.Application;

namespace Lab.HangfireApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            var config = new HttpConfiguration();
            SwaggerConfig.Register(config);
            HangfireConfig.Register(app);
            config.Routes.MapHttpRoute("DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional}
                                      );

            app.UseWelcomePage("/");
            app.UseWebApi(config);
            app.UseErrorPage();
        }
    }
}