using System;
using System.Threading;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.HangfireUnitTest
{
    [TestClass]
    public class BackgroundJobServer_IntegrateTest
    {
        private static BackgroundJobServer s_jobServer;
        private static JobStorage s_storage;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var options = new BackgroundJobServerOptions
            {
                SchedulePollingInterval = new TimeSpan(0, 0, 0),
            };
            s_storage = new MemoryStorage();
            s_jobServer = new BackgroundJobServer(options, s_storage);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_jobServer?.Dispose();
        }

        [TestMethod]
        public void 集成測試()
        {
            var job    = new DemoJob();
            var client = new BackgroundJobClient(s_storage);
            job.JobClient = client;
            job.EnqueueAction();
            Thread.Sleep(5000);
        }
    }
}
