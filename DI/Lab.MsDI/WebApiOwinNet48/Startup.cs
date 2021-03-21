using System.Web.Http;
using Owin;

namespace WebApiOwinNet48
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var httpConfig = new HttpConfiguration();
            DependencyInjectionConfig.Register(httpConfig);
            WebApiConfig.Register(httpConfig);
            //app.UseErrorPage();
            //app.UseWelcomePage("/Welcome");
            // app.Use<LabMiddleware>();
            app.UseWebApi(httpConfig);
        }
    }
}