using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace ServerSwashbuckle.Controllers
{
    [RoutePrefix("api/file")]
    public class FileController : ApiController
    {
        [HttpGet]
        [Route("download")]
        public async Task<IHttpActionResult> Download(string fileName)
        {
            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            var exists = Directory.Exists(root);
            if (!exists)
            {
                Directory.CreateDirectory("App_Data");
            }

            var filePath = Path.Combine(root, fileName);
            try
            {
                if (!File.Exists(filePath))
                {
                    return this.NotFound();
                }

                var fileStream = new FileStream(filePath, FileMode.Open);
                var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileName));
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
                return new ResponseMessageResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = content
                });
            }
            catch (Exception e)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(e.Message)
                });
            }
        }
    }
}