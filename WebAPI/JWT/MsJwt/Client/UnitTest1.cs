using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Server;

namespace Client
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
        public void AccessProtectResource()
        {
            var loginUrl = "api/token";
            var queryUrl = "api/value";

            var tokenResponse = s_client.PostAsJsonAsync(loginUrl,
                                                         new LoginData
                                                         {
                                                             UserName = "yao",
                                                             Password = "1234"
                                                         })
                                        .Result;
            Assert.AreEqual(HttpStatusCode.OK, tokenResponse.StatusCode);

            var token = tokenResponse.Content.ReadAsStringAsync().Result;
            s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var queryResponse = s_client.GetAsync(queryUrl).Result;
            Assert.AreEqual(HttpStatusCode.OK, queryResponse.StatusCode);
            var result = queryResponse.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("value", result);
        }

        //[TestMethod]
        //public void Timeout_Should_Be_Unauthorized()
        //{
        //    var loginUrl = "api/token";
        //    var queryUrl = "api/value";
        //    JwtManager.Now = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);

        //    var tokenResponse = s_client.PostAsJsonAsync(loginUrl,
        //                                                 new LoginData
        //                                                 {
        //                                                     UserName = "yao",
        //                                                     Password = "1234"
        //                                                 })
        //                                .Result;
        //    Assert.AreEqual(HttpStatusCode.OK, tokenResponse.StatusCode);

        //    var token = tokenResponse.Content.ReadAsStringAsync().Result;
        //    s_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //    JwtManager.Now = JwtManager.Now.Value.AddMinutes(30);
        //    var queryResponse = s_client.GetAsync(queryUrl).Result;
        //    Assert.AreEqual(HttpStatusCode.Unauthorized, queryResponse.StatusCode);
        //    JwtManager.Now = null;
        //}
        public class LoginData
        {
            public string UserName { get; set; }

            public string Password { get; set; }
        }
    }
}