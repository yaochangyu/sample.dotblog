using System;
using System.Threading;
using Hangfire;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.HangfireUnitTest
{
    [TestClass]
    public class BackgroundJobServerOwin_IntegrateTest
    {
        private const  string      HOST_ADDRESS = "http://localhost:9527";
        private static IDisposable s_webApp;

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_webApp?.Dispose();
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Hangfire service start...");
        }

        [TestMethod]
        public void 集成測試()
        {
            var job    = new DemoJob();
            var client = new BackgroundJobClient();
            job.JobClient = client;
            job.EnqueueAction();
            Thread.Sleep(5000);
        }
    }
}