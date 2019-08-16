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
        public void Server_DeflateCompress_Client_DeflateDecompress_should_be_yao()
        {
            Console.WriteLine("用戶端用訪問伺服器→伺服器用Deflate壓縮資料→Client解壓縮，驗證解壓縮結果是否包含關鍵字");

            var url = "api/test/DeflateCompression/yao";
            var response = MsTestHook.Client.GetAsync(url).Result;
            var content = response.Content.ReadAsByteArrayAsync().Result;
            var decompress = Deflate.Decompress(content);
            var result = Encoding.UTF8.GetString(decompress);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(true, result.Contains("yao"));
        }

        [TestMethod]
        public void Client_DeflateCompress_Server_DeflateDecompress()
        {
            Console.WriteLine("用戶端用Deflate壓縮資料→伺服器端解壓縮後回傳結果→驗證解壓縮結果和Client壓縮前是否相同");
            var url = "api/test/DeflateDecompression";
            var builder = CreateData();

            var contentBytes = Encoding.UTF8.GetBytes(builder);
            var zipContent = Deflate.Compress(contentBytes);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new ByteArrayContent(zipContent)
            };
            var response = MsTestHook.Client.SendAsync(request).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(builder, result);
        }

        [TestMethod]
        public void Server_GZipCompress_Client_GZipDecompress_should_be_yao()
        {
            Console.WriteLine("用戶端用訪問伺服器→伺服器用GZip壓縮資料→Client解壓縮，驗證解壓縮結果是否包含關鍵字");

            var url = "api/test/GZipCompression/yao";
            var response = MsTestHook.Client.GetAsync(url).Result;
            var content = response.Content.ReadAsByteArrayAsync().Result;
            var decompress = GZip.Decompress(content);
            var result = Encoding.UTF8.GetString(decompress);
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(true, result.Contains("yao"));
        }
        [TestMethod]
        public void Client_GZipCompress_Server_GZipDecompress()
        {
            Console.WriteLine("用戶端用GZip壓縮資料→伺服器端解壓縮後回傳結果→驗證解壓縮結果和Client壓縮前是否相同");
            var url = "api/test/GZipDecompression";
            var builder = CreateData();

            var contentBytes = Encoding.UTF8.GetBytes(builder);
            var zipContent = GZip.Compress(contentBytes);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new ByteArrayContent(zipContent)
            };
            var response = MsTestHook.Client.SendAsync(request).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(builder, result);
        }

        private static string CreateData()
        {
            var builder = new StringBuilder();
            var times = 0;

            for (var i = 0; i < 1000; i++)
            {
                builder.AppendLine($"{(i + 1).ToString("0000")},您好," +
                                   $"我是 {Name.FullName()}," +
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