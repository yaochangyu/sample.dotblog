using System.Web.Http;
using Lab.HangfireServer.Filters;
using Owin;

namespace Lab.HangfireServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            var config = new HttpConfiguration();
            config.Filters.Add(new ErrorHandlerAttribute());
            HangfireConfig.Register(app);
            SwaggerConfig.Register(config);
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