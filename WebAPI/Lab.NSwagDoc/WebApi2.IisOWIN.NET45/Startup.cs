using System.Web.Http;
using NSwag.AspNet.Owin;
using Owin;

namespace WebApi2.IisOWIN.NET45
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //var config = new HttpConfiguration();
            var config = GlobalConfiguration.Configuration;
            app.UseSwaggerUi3(typeof(Startup).Assembly, settings =>
                                                        {
                                                            settings.GeneratorSettings.DefaultUrlTemplate =
                                                                "api/{controller}/{id?}";

                                                            settings.PostProcess = document =>
                                                                                   {
                                                                                       document.Info.Title =
                                                                                           "WebAPI OWIN Demo";
                                                                                   };
                                                        });

            //app.UseWebApi(config);
            //config.MapHttpAttributeRoutes();
            config.EnsureInitialized();
        }
    }
}