using System.Web;
using System.Web.Http;

namespace WebApiNet48
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configure(DependencyInjectionConfig.Register);
        }
    }
}