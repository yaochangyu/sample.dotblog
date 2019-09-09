using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace Client
{
    namespace Server1
    {
    }

    [TestClass]
    public class DefaultController2UnitTest
    {
        //private const           string      HOST_ADDRESS = "http://localhost:8001";
        private const           string     HOST_ADDRESS = "https://localhost:44378";
        private static readonly ILogger    s_logger;
        private static          HttpClient s_client;

        static DefaultController2UnitTest()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        [TestMethod]
        public void Default2_Cancel()
        {
            var cancelToken = new CancellationTokenSource();

            var url = "api/default2";

            //var cancelUrl = "api/default2/canncel";
            var request = s_client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancelToken.Token);

            //Task.Delay(2000).Wait();
            Task.Delay(1000).Wait();

            var response =
                request.ContinueWith(task =>
                                     {
                                         var status =
                                             $"Status      = {task.Status}\r\n"      +
                                             $"IsCanceled  = {task.IsCanceled}\r\n"  +
                                             $"IsCompleted = {task.IsCompleted}\r\n" +
                                             $"IsFaulted   = {task.IsFaulted}";

                                         s_logger.Trace(status);
                                         return task;
                                     },
                                     TaskContinuationOptions.None
                                    );
            cancelToken.Cancel();
            Task.Delay(1000).Wait();
            Task.WaitAll(response);
        }

        [TestMethod]
        public async Task TestAsync()
        {
            var handler = new TimeoutHandler
            {
                DefaultTimeout = TimeSpan.FromSeconds(5),
                InnerHandler   = new HttpClientHandler()
            };
            var url = "api/default";

            using (var cts = new CancellationTokenSource())
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(HOST_ADDRESS);
                client.Timeout     = Timeout.InfiniteTimeSpan;

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Uncomment to test per-request timeout
                //request.SetTimeout(TimeSpan.FromSeconds(5));

                // Uncomment to test that cancellation still works properly
                //cts.CancelAfter(TimeSpan.FromSeconds(2));

                using (var response = await client.SendAsync(request, cts.Token))
                {
                    Console.WriteLine(response.StatusCode);
                }
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            s_client.Dispose();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            s_client             = new HttpClient();
            s_client.BaseAddress = new Uri(HOST_ADDRESS);
        }
    }
}