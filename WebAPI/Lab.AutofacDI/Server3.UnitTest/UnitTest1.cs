using System;
using System.Net.Http;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Server3.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private const  string      HOST_ADDRESS = "http://localhost:9527";
        private static IDisposable s_webApp;
        private static HttpClient  s_client;

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp.Dispose();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Web API started!");
            s_client             = new HttpClient();
            s_client.BaseAddress = new Uri(HOST_ADDRESS);
            Console.WriteLine("HttpClient started!");
        }

        [TestMethod]
        public void When_Call_Get_Should_Be_Product()
        {
            var url      = "api/Default";
            var response = s_client.GetAsync(url).Result;
            var result   = response.Content.ReadAsAsync<string>().Result;
            Assert.AreEqual("Product", result);
        }
    }
}
