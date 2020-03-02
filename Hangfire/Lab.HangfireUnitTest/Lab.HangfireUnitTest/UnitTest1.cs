using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Lab.HangfireUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void 列舉被測目標目與相依物件互動的參數()
        {
            var client  = Substitute.For<IBackgroundJobClient>();
            var demoJob = new DemoJob(client);
            demoJob.EnqueueAction();

            var calls = client.ReceivedCalls();
            foreach (var call in calls)
            {
                var arguments = call.GetArguments();
            }
        }

        [TestMethod]
        public void 驗證有呼叫Create方法()
        {
            //arrange
            var client  = Substitute.For<IBackgroundJobClient>();
            var demoJob = new DemoJob(client);

            //act
            demoJob.EnqueueAction();

            //assert
            client.Received()
                  .Create(Arg.Is<Job>(p => p.Method.Name    == "Action"),
                          Arg.Is<EnqueuedState>(p => p.Name == "Enqueued"));
        }
    }
}