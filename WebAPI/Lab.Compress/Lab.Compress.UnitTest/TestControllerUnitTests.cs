using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Faker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.Compress.UnitTest
{
    [TestClass]
    public class TestControllerUnitTests
    {
        [TestMethod]
        public void Get()
        {
            var url        = "api/test/yao";
            var response   = MsTestHook.Client.GetAsync(url).Result;
            var content    = response.Content.ReadAsByteArrayAsync().Result;
            var decompress = Deflate.Decompress(content);
            var result     = Encoding.UTF8.GetString(decompress);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(true,                result.Contains("yao"));
        }

        [TestMethod]
        public void Post()
        {
            var url     = "api/test";
            var builder = new StringBuilder();
            var times   = 0;

            for (var i = 0; i < 1000; i++)
            {
                builder.AppendLine($"{(i + 1).ToString("0000")},您好,"   +
                                   $"我是 {Name.FullName()},"            +
                                   $"今年 {RandomNumber.Next(1, 100)}歲," +
                                   $"家裡住在 {Address.Country()},{Address.City()}");
                times++;
            }

            builder.AppendLine($"共出現:{times}次");
            builder.AppendLine($"目前時間:{DateTime.Now.ToLocalTime()}");
            builder.AppendLine("Id:yao");

            var contentBytes = Encoding.UTF8.GetBytes(builder.ToString());
            var zipContent   = Deflate.Compress(contentBytes);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new ByteArrayContent(zipContent)
            };
            var response = MsTestHook.Client.SendAsync(request).Result;
            var result   = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(builder.ToString(),  result);
        }
    }
}