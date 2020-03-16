using System.Web.Http;
using Owin;
using Quartz.Impl;
using Quartzmin;

namespace Lab.QuartzConsoleApp
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=316888
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("DefaultApi",
                                       "api/{controller}/{id}",
                                       new { id = RouteParameter.Optional }
                                      );
            app.UseQuartzmin(new QuartzminOptions()
            {
                Scheduler = StdSchedulerFactory.GetDefaultScheduler().Result
            });

            app.UseWelcomePage("/");
            app.UseWebApi(config);
            app.UseErrorPage();
        }
    }
}