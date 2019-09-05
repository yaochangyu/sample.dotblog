using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using Server.EntityModel;

namespace Client
{
    [TestClass]
    public class UnitTest1
    {
        private const           string      HOST_ADDRESS = "http://localhost:8001";
        private static readonly ILogger     s_logger;
        private static          IDisposable s_webApp;
        private static          HttpClient  s_client;

        static UnitTest1()
        {
            if (s_logger == null)
            {
                s_logger = LogManager.GetCurrentClassLogger();
            }
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp.Dispose();
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

            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);

            //Console.WriteLine("Web API started!");
            s_logger.Trace("Web API Start");
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
            Task.Delay(1000).Wait();
            Task.WaitAll(response);
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
                                             $"Status      = {task.Status}\r\n" +
                                             $"IsCanceled  = {task.IsCanceled}\r\n" +
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
    }
}