using System;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.HangfireUnitTest
{
    [TestClass]
    public class MsTestHook
    {
        private const  string      HOST_ADDRESS = "http://localhost:9527";
        private static IDisposable s_webApp;

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp?.Dispose();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Hangfire service start...");
        }
    }
}