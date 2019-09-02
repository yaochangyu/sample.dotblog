using System.Web.Http;
using Owin;

namespace Server.UnitTest
{
    public class Startup
    {
        public static AutofacManager AutofacManager { get; internal set; }

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            ConfigureRoute(config);

            ConfigureAutofac(config);

            app.UseWebApi(config);
        }

        private static void ConfigureAutofac(HttpConfiguration config)
        {
            AutofacManager = new AutofacManager(config);
            var builder = AutofacManager.CreateApiBuilder();
            AutofacManager.CreateContainer(builder);
        }

        private static void ConfigureRoute(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional});
        }
    }
}