using System;
using System.Net.Http;
using AspNetFx.WebApi;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AspNetFx.WebApi.Test
{
    [TestClass]
    public class HttpContextProviderConfigurationTest
    {
        private const string HOST_ADDRESS = "http://localhost:8002";
        private IDisposable _webApp;
        private HttpClient _client;

        [TestInitialize]
        public void TestInitialize()
        {
            _webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            _client = new HttpClient();
            _client.BaseAddress = new Uri(HOST_ADDRESS);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _client?.Dispose();
            _webApp?.Dispose();
        }

        [TestMethod]
        public void TestOwinProviderConfiguration()
        {
            // 測試是否正確使用 OWIN Provider
            var url = "api/values";
            var response = _client.GetAsync(url).Result;
            
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            
            // 由於 OWinHttpContextProvider 目前拋出 NotImplementedException
            // 這個測試會驗證啟動時設定是否正確
            var content = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Response content: {content}");
            
            // 實際的測試會在 OWinHttpContextProvider 完成實作後進行
        }

        [TestMethod]
        public void TestConfigurationPersistence()
        {
            // 驗證設定在啟動後是否持續有效
            var factory = new HttpContextProviderFactory();
            var provider = factory.CreateProvider();
            
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(OWinHttpContextProvider));
        }
    }
}