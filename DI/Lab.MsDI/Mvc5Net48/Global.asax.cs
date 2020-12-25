using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;

[assembly: PreApplicationStartMethod(typeof(Mvc5Net48.Global), "InitModule")]

namespace Mvc5Net48
{
    public class Global : HttpApplication
    {
        public static void InitModule()
        {
            RegisterModule(typeof(ServiceScopeModule));
        }
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configure(DependencyInjectionConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);            
        }
    }
}