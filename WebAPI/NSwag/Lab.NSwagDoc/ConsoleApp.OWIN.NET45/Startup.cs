using System.Web.Http;
using NSwag.AspNet.Owin;
using Owin;

namespace ConsoleApp.OWIN.NET45
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            app.UseSwaggerUi3(typeof(Startup).Assembly,
                              settings =>
                              {
                                  settings.GeneratorSettings.DefaultUrlTemplate =
                                      "api/{controller}/{id?}";

                                  settings.PostProcess = document =>
                                                         {
                                                             document.Info.Title =
                                                                 "WebAPI OWIN Demo";
                                                         };
                              });
            WebApiConfig.Register(config);
            app.UseErrorPage();
            app.UseWelcomePage("/Welcome");
            app.UseWebApi(config);
            config.EnsureInitialized();
        }
    }
}