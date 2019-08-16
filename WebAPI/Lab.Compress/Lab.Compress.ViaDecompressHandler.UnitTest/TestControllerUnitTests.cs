using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Faker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.Compress.ViaDecompressHandler.UnitTest
{
    [TestClass]
    public class TestControllerUnitTests
    {
        [TestMethod]
        public void Client_DeflateCompressHttpContent_Server_DeflateHandlerDecompress()
        {
            var url  = "api/test/Decompression";
            var data = CreateData();

            var content = new CompressContent(new StringContent(data, Encoding.UTF8, "text/plain"),
                                              CompressMethod.Deflate);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            var response = MsTestHook.Client.SendAsync(request).Result;
            var result   = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(data,                result);
        }

        [TestMethod]
        public void Client_GZipCompressHttpContent_Server_GZipHandlerDecompress()
        {
            var url  = "api/test/Decompression";
            var data = CreateData();

            var content = new CompressContent(new StringContent(data, Encoding.UTF8, "text/plain"),
                                              CompressMethod.GZip);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            var client   = MsTestHook.Client;
            var response = client.SendAsync(request).Result;
            var result   = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(data,                result);
        }

        private static string CreateData()
        {
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
            return builder.ToString();
        }
    }
}