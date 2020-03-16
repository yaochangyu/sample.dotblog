using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Client
{
    [TestClass]
    public class UnitTest1
    {
        private const string AHTH_HOST_ADDRESS = "http://localhost:8001";
        private const string RESOURCE_HOST_ADDRESS = "http://localhost:8002";
        private static IDisposable s_authWebApp;
        private static IDisposable s_resourceWebApp;
        private static HttpClient s_authClient;
        private static HttpClient s_resourceClient;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            s_authWebApp = WebApp.Start<AuthStartup>(AHTH_HOST_ADDRESS);
            s_resourceWebApp = WebApp.Start<ResourceStartup>(RESOURCE_HOST_ADDRESS);

            s_authClient = new HttpClient();
            s_authClient.BaseAddress = new Uri(AHTH_HOST_ADDRESS);

            s_resourceClient = new HttpClient();
            s_resourceClient.BaseAddress = new Uri(RESOURCE_HOST_ADDRESS);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_authWebApp.Dispose();
            s_resourceWebApp.Dispose();
        }

        [TestMethod]
        public void Access_Resource_Should_Be_Value()
        {
            var loginUrl = "api/token";
            var queryUrl = "api/value";

            var tokenResponse = s_authClient.PostAsJsonAsync(loginUrl,
                                                             new LoginData
                                                             {
                                                                 UserName = "yao",
                                                                 Password = "1234"
                                                             })
                                            .Result;
            Assert.AreEqual(HttpStatusCode.OK, tokenResponse.StatusCode);

            var token = tokenResponse.Content.ReadAsStringAsync().Result;
            s_resourceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var queryResponse = s_resourceClient.GetAsync(queryUrl).Result;
            Assert.AreEqual(HttpStatusCode.OK, queryResponse.StatusCode);
            var result = queryResponse.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("value", result);
        }
    }
}