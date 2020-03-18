using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using NSwag.AspNet.Owin;

namespace WebApi2.IIS.NET45
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes
                      .MapOwinPath("swagger", app =>
                                              {
                                                  app.UseSwaggerUi3(typeof(WebApiApplication).Assembly,
                                                                    settings =>
                                                                    {
                                                                        settings.MiddlewareBasePath = "/swagger";

                                                                        settings.GeneratorSettings
                                                                                .DefaultUrlTemplate =
                                                                            "api/{controller}/{id}";
                                                                        settings.PostProcess = document =>
                                                                                               {
                                                                                                   document.Info.Title =
                                                                                                       "WebAPI NSwag";
                                                                                               };
                                                                    });
                                              });
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}