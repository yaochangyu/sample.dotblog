using System.Web.Http;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(AspNetFx.WebApi.Startup))]

namespace AspNetFx.WebApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 設定使用 OWIN HttpContext Provider
            HttpContextProviderConfiguration.UseOwinProvider();
            
            var config = new HttpConfiguration();
            
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            app.UseWebApi(config);
        }
    }
}