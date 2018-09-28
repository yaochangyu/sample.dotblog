using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApi.Proxies.Clients;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        private readonly ValuesClient _client;

        public UnitTest1()
        {
            if (this._client == null)
            {
                this._client = new ValuesClient();
                this._client.HttpClient.BaseAddress = new Uri("http://localhost:56364");
            }
        }

        [TestMethod]
        public void Put_Test()
        {
            var responseMessage = this._client.PutAsync(1, "2").Result;
            var success = responseMessage.StatusCode == HttpStatusCode.NoContent;
            Assert.AreEqual(true, success);
        }
    }
}