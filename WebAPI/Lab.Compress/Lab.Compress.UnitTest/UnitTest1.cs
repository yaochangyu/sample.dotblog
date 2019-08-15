using System;
using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.Compress.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Get()
        {
            var url = "api/test/yao";
            var response = MsTestHook.Client.GetAsync(url).Result;
            var content = response.Content.ReadAsByteArrayAsync().Result;
            var decompress = Deflate.Decompress(content);
            string result = System.Text.Encoding.UTF8.GetString(decompress);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            var hasKeyWord = result.Contains("yao");
            Assert.AreEqual(true,hasKeyWord);
        }
    }
}
