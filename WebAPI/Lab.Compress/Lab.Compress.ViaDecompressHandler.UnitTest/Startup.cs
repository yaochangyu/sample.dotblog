using System.Web.Http;
using Owin;

namespace Lab.Compress.ViaDecompressHandler.UnitTest
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