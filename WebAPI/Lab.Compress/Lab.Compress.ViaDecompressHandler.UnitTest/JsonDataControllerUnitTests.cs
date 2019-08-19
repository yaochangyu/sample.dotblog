using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web;
using Faker;
using Lab.Compress.ViaDecompressHandler.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Lab.Compress.ViaDecompressHandler.UnitTest
{
    [TestClass]
    public class JsonDataControllerUnitTests
    {
        private static readonly string s_filePath = @"member.json";


        [ClassInitialize]
        public static void Startup(TestContext context)
        {
            if (!File.Exists(s_filePath))
            {
                CreateTestFile();
            }
        }

        [TestMethod]
        public void Post()
        {
            var url  = "api/JsonData/Post";
            var data = GetDataFromFile();

            var content = new CompressContent(new ObjectContent<IEnumerable<Member>>(data,
                                                                                     new JsonMediaTypeFormatter(),
                                                                                     MimeType.Application.Json
                                                                                     ),
                                              CompressMethod.Deflate);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            var response = MsTestHook.Client.SendAsync(request).Result;
            Assert.AreEqual(response.StatusCode, HttpStatusCode.NoContent);
        }

        IEnumerable<Member> GetDataFromFile()
        {
            return JsonConvert.DeserializeObject<IEnumerable<Member>>(File.ReadAllText(s_filePath));
        }
        private static IEnumerable<Member> CreateTestFile()
        {
            var results = new List<Member>();
            for (var i = 0; i < 1000000; i++)
            {
                results.Add(new Member
                {
                    Id      = Guid.NewGuid(),
                    Name    = Name.FullName(),
                    Age     = RandomNumber.Next(1, 120),
                    Address = $"{Address.StreetAddress(true)}"
                });
            }

            File.WriteAllText(s_filePath, JsonConvert.SerializeObject(results));
            return results;
        }
    }
}