using System;
using System.Net;
using System.Net.Http;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetFx.WebApi.Test
{
    [TestClass]
    public class UnitTest1
    {
        private const string HOST_ADDRESS = "http://localhost:8001";
        private static IDisposable s_webApp;
        private static HttpClient s_client;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Web API started!");
            s_client = new HttpClient();
            s_client.BaseAddress = new Uri(HOST_ADDRESS);
            Console.WriteLine("HttpClient started!");
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp.Dispose();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var url = "api/values/1";
            var response = s_client.GetAsync(url).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var content = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("\"value\"", content);
        }
    }
}