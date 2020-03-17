using System.Web.Http;
using Owin;

namespace ConsoleApp.OWIN.NET45
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var configuration = new HttpConfiguration();
            WebApiConfig.Register(configuration);
            app.UseErrorPage();
            app.UseWelcomePage("/Welcome");
            app.UseWebApi(configuration);
        }
    }
}