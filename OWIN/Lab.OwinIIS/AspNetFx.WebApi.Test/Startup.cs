using System.Web.Http;
using AspNetFx.WebApi;
using Owin;

namespace AspNetFx.WebApi.Test
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 設定使用 OWIN HttpContext Provider
            var configuration = new HttpConfiguration();
            WebApiConfig.Register(configuration);
            //app.UseErrorPage();
            //app.UseWelcomePage("/Welcome");
            app.UseWebApi(configuration);
        }
    }
}
