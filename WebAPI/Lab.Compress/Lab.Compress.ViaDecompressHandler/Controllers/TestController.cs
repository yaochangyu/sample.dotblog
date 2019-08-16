using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Results;
using Faker;

namespace Lab.Compress.ViaDecompressHandler
{
    public class TestController : ApiController
    {

        [HttpPost]
        public IHttpActionResult Decompression()
        {
            //var sourceStream = this.Request.Content.ReadAsStreamAsync().Result;
            //StreamReader reader = new StreamReader(sourceStream);
            //string result = reader.ReadToEnd();

            var content = this.Request.Content.ReadAsByteArrayAsync().Result;
            var result = Encoding.UTF8.GetString(content);
            return new ResponseMessageResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(result, Encoding.UTF8)
            });
            return this.Ok();
        }
    }
}
