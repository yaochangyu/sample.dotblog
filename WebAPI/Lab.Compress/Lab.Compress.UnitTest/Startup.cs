using System.Web.Http;
using Owin;

namespace Lab.Compress.UnitTest
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var configuration = new HttpConfiguration();
            WebApiConfig.Register(configuration);
            //app.UseErrorPage();
            //app.UseWelcomePage("/Welcome");
            app.UseWebApi(configuration);
        }
    }
}