using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Owin;

namespace Tfs.WebHook.UnitTest
{
    [TestClass]
    public class MessagesControllerTests
    {
        private readonly string s_baseUri = "http://localhost:999";
        private TestServer _server;

        [TestInitialize]
        public void Setup()
        {
            this._server = TestServer.Create(app =>
                                             {
                                                 var startup = new Startup();
                                                 startup.Configuration(app);
                                                 app.UseErrorPage(); // See Microsoft.Owin.Diagnostics
                                                 app.UseWelcomePage("/Welcome"); // See Microsoft.Owin.Diagnostics

                                                 var config = new HttpConfiguration();
                                                 app.UseWebApi(config);
                                             });
        }

        protected virtual async Task<HttpResponseMessage> GetAsync(string uri)
        {
            return await this._server.CreateRequest(uri)
                             .GetAsync();
        }

        protected virtual async Task<HttpResponseMessage> PostAsync<TModel>(string uri, TModel model)
        {
            return await this._server.CreateRequest(uri)
                             .And(r => r.Content = new ObjectContent(typeof(TModel), model,
                                                                     new JsonMediaTypeFormatter()))
                             .PostAsync();
        }

        [TestMethod]
        public async Task Post_BuildCompleted_Test()
        {
            var root = JsonConvert.DeserializeObject<TfsRootObject>(TestHook.BuildCompleted);
            var response = await this.PostAsync(this.s_baseUri + "/api/messages", root);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Post_BuildCompleted_Content_Test()
        {
            var response = await this._server.CreateRequest(this.s_baseUri + "/api/messages/p2")
                                     .And(p => p.Content = new StringContent(TestHook.BuildCompleted))
                                     .PostAsync();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}