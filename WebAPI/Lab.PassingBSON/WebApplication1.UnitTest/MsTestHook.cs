using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;

namespace WebApplication1.UnitTest
{
    [TestClass]
    public class MsTestHook
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Server = TestServer.Create(app =>
                                       {
                                           var startup = new Startup();
                                           startup.Configuration(app);
                                           app.UseErrorPage(); // See Microsoft.Owin.Diagnostics
                                           app.UseWelcomePage("/Welcome"); // See Microsoft.Owin.Diagnostics
                                           var config = new HttpConfiguration();

                                           WebApiConfig.Register(config);

                                           app.UseWebApi(config);
                                       });
        }

        public static TestServer Server { get; set; }
    }
}
