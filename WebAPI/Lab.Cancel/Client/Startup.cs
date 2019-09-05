using System.Web.Http;
using Owin;
using Server1;

namespace Client
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var httpConfig = new HttpConfiguration();
            Route.Configure(httpConfig);
            app.UseErrorPage();

            //app.UseWelcomePage("/Welcome");
            app.UseWebApi(httpConfig);
        }
    }
}