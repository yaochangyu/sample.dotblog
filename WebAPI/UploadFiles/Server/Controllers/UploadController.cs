using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Server.Models;

namespace Server.Controllers
{
    [RoutePrefix("api/upload")]
    public class UploadController : ApiController
    {
        [HttpPost]
        [Route("upload")]
        public async Task<IHttpActionResult> Upload()
        {
            if (!this.Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            var exists = Directory.Exists(root);
            if (!exists)
            {
                Directory.CreateDirectory("App_Data");
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await this.Request.Content.ReadAsMultipartAsync(provider);

                var uploadResponse = new UploadResponse();
                foreach (var content in provider.Contents)
                {
                    var fileName = content.Headers.ContentDisposition.FileName.Trim('\"');
                    var fileArray = await content.ReadAsByteArrayAsync();

                    var outputPath = Path.Combine(root, fileName);
                    using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        await output.WriteAsync(fileArray, 0, fileArray.Length);
                    }

                    uploadResponse.Names.Add(fileName);
                    uploadResponse.FileNames.Add(outputPath);
                    uploadResponse.ContentTypes.Add(content.Headers.ContentType.MediaType);
                }

                return this.Ok(uploadResponse);
            }
            catch (Exception e)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(e.Message)
                });
            }
        }

        [HttpPost]
        [Route("UploadFormData")]
        public async Task<IHttpActionResult> UploadFormData()
        {
            if (!this.Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            var exists = Directory.Exists(root);
            if (!exists)
            {
                Directory.CreateDirectory("App_Data");
            }

            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                await this.Request.Content.ReadAsMultipartAsync(provider);
                var uploadResponse = new UploadResponse();
                uploadResponse.Description = provider.FormData["description"];

                foreach (var content in provider.FileData)
                {
                    var fileName = content.Headers.ContentDisposition.FileName.Trim('\"');
                    uploadResponse.Names.Add(fileName);
                    uploadResponse.FileNames.Add(content.LocalFileName);
                    uploadResponse.ContentTypes.Add(content.Headers.ContentType.MediaType);
                }

                return this.Ok(uploadResponse);
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