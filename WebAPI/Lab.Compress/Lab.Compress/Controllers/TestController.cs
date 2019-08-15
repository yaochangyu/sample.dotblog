using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Faker;
using Ionic.Zlib;
using Lab.Compress.Filters;
using Newtonsoft.Json;

namespace Lab.Compress.Controllers
{
    public class TestController : ApiController
    {
        //[GZipCompression]
        [DeflateCompression]
        public IHttpActionResult Get(string id)
        {
            var builder = new StringBuilder();
            var times   = 0;
            for (var i = 0; i < 1000; i++)
            {
                builder.AppendLine($"{(i + 1).ToString("0000")},您好," +
                                   $"我是 {Name.FullName()}," +
                                   $"今年 {RandomNumber.Next(1,100)}歲," +
                                   $"家裡住在 {Address.Country()},{Address.City()}");
                times++;
            }

            builder.AppendLine($"共出現:{times}次");
            builder.AppendLine($"目前時間:{DateTime.Now.ToLocalTime()}");
            builder.AppendLine($"Id:{id}");
            var content = builder.ToString();

            return new ResponseMessageResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content    = new StringContent(content, Encoding.UTF8)
            });
        }

        public async Task<IHttpActionResult> Get()
        {
            var people = new List<PersonModel>
            {
                new PersonModel
                {
                    FirstName = "Test",
                    LastName  = "One",
                    Age       = 25
                },
                new PersonModel
                {
                    FirstName = "Test",
                    LastName  = "Two",
                    Age       = 45
                }
            };
            using (var handler = new HttpClientHandler())
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                using (var client = new HttpClient(handler, false))
                {
                    var json      = JsonConvert.SerializeObject(people);
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    var ms        = new MemoryStream();
                    using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                    {
                        gzip.Write(jsonBytes, 0, jsonBytes.Length);
                    }

                    ms.Position = 0;
                    var content = new StreamContent(ms);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    content.Headers.ContentEncoding.Add("gzip");
                    var response = await client.PostAsync("http://localhost:54425/api/Gzipping", content);
                    var results  = await response.Content.ReadAsAsync<IEnumerable<PersonModel>>();
                    Debug.WriteLine(string.Join(", ", results));
                }
            }

            return this.Ok();
        }

        public IHttpActionResult Post()
        {
            return this.Ok();
        }
    }
}