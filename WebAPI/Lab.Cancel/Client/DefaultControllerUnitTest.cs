using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Server1.EntityModel;

namespace Client
{
    [TestClass]
    public class DefaultControllerUnitTest
    {
        //private const           string      HOST_ADDRESS = "http://localhost:8001";
        private const           string     HOST_ADDRESS = "https://localhost:44378";
        private static readonly ILogger    s_logger;
        private static          HttpClient s_client;

        static DefaultControllerUnitTest()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_client.Dispose();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            using (var dbDbContext = TestDbContext.Create())
            {
                if (dbDbContext.Database.Exists() == false)
                {
                    dbDbContext.Database.Initialize(true);
                }
            }

            s_client             = new HttpClient();
            s_client.BaseAddress = new Uri(HOST_ADDRESS);
        }

        [TestMethod]
        public void Cancel()
        {
            var cancelToken = new CancellationTokenSource();

            var url     = "api/Default";
            var request = s_client.GetAsync(url, cancelToken.Token);

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
            //s_client.CancelPendingRequests();
            Task.Delay(1000).Wait();
            Task.WaitAll(response);
        }

        [TestMethod]
        public async Task Cancel2()
        {
            var url = "api/Default";
            using (var client = new HttpClient())
            using (var cts = new CancellationTokenSource())
            {
                client.BaseAddress = new Uri(HOST_ADDRESS);
                Task.Delay(3000).ContinueWith(t =>
                                              {
                                                  Console.Out.WriteLine("Cancel");
                                                  client.CancelPendingRequests();
                                                  cts.Cancel();
                                              });

                try
                {
                    Console.Out.WriteLine("Starting Download");

                    using (var result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                    using (var output = new MemoryStream())
                    using (var stream = await result.Content.ReadAsStreamAsync())
                    using (cts.Token.Register(() => stream.Close()))
                    {
                        var buffer = new byte[80000];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            output.Write(buffer, 0, bytesRead);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("Cancelled");
                }
            }
        }

        [TestMethod]
        public async Task CancelAsync()
        {
            var cancelToken = new CancellationTokenSource();

            var url     = "api/Default";
            var request = s_client.GetAsync(url, cancelToken.Token);

            //Task.Delay(2000).Wait();
            await Task.Delay(1000);

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
            await Task.Delay(1000);
            Task.WaitAll(response);
        }

        [TestMethod]
        [Ignore]
        public async Task Get()
        {
            var url      = "api/Default";
            var request  = s_client.GetAsync(url);
            var response = await request;
            var content  = response.Content.ReadAsAsync<string>().Result;
            Assert.AreEqual("10", content);
        }

        [TestMethod]
        public void InsertCancel()
        {
            var cancelToken = new CancellationTokenSource();

            var url      = "api/Default";
            var products = new List<Product>();
            for (var i = 0; i < 10000; i++)
            {
                products.Add(new Product {Id = Guid.NewGuid(), Name = i.ToString()});
            }

            var request = s_client.PostAsJsonAsync(url, products, cancelToken.Token);

            Task.Delay(10000).Wait();

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
            Task.Delay(5000).Wait();
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
    }
}