using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;

namespace WebApplication1.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private readonly string s_baseUri = "http://test:9999";

        protected virtual async Task<HttpResponseMessage> GetAsync(string uri)
        {
            return await MsTestHook.Server.CreateRequest(uri)
                                   .GetAsync();
        }

        protected virtual async Task<HttpResponseMessage> PostAsync<TModel>(string uri, TModel model)
        {
            return await MsTestHook.Server.CreateRequest(uri)
                                   .And(request => request.Content =
                                                       new ObjectContent(typeof(TModel), model,
                                                                         new JsonMediaTypeFormatter()))
                                   .PostAsync();
        }

        [TestMethod]
        public async Task Get_BSON()
        {
            var url = $"{this.s_baseUri}/api/values";
            HttpClient client = MsTestHook.Server.HttpClient;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));

            //取得資料
            var response = client.GetAsync(url).Result;

            //還原資料
            var formatters = new MediaTypeFormatter[] {new BsonMediaTypeFormatter()};
            var result = response.Content.ReadAsAsync<IEnumerable<string>>(formatters).Result;
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Post_BSON()
        {
            var url = $"{this.s_baseUri}/api/values";
            var client = MsTestHook.Server.HttpClient;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));
            HttpContent content = new StringContent("哩來", Encoding.UTF8, "application/json");
            var response = client.PostAsync(url, content).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
        }
    }
}