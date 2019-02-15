using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Server.Models;

namespace Client
{
    [TestClass]
    public class UnitTest1
    {
        private const string HOST_ADDRESS = "http://localhost:9527";
        private static IDisposable s_webApp;
        private static HttpClient s_client;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Web API started!");
            s_client = new HttpClient();
            s_client.BaseAddress = new Uri(HOST_ADDRESS);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp.Dispose();
        }

        [TestMethod]
        public void Updload_TEST()
        {
            var url = "/api/file/upload";
            var fileNames = new[] {"例外1.txt", "例外2.txt"};
            using (var content = new MultipartFormDataContent())
            {
                foreach (var fileName in fileNames)
                {
                    var fileMimeType = MimeMapping.GetMimeMapping(fileName);
                    var fileBytes = File.ReadAllBytes(fileName);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileMimeType);
                    content.Add(fileContent, fileName, fileName);
                }

                var response = s_client.PostAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsAsync<UploadResponse>().Result;
                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public void Upload_TEST2()
        {
            var url = "/api/file/UploadFormData";
            var fileName = "例外1.txt";
            var mimeType = MimeMapping.GetMimeMapping(fileName);
            using (var content = new MultipartFormDataContent())
            {
                FileStream fileStream = new FileStream(fileName, FileMode.Open);

                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {FileName = fileName};
                content.Add(streamContent, "File", fileName);
                s_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                var response = s_client.PostAsync(url, content).Result;
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsAsync<UploadResponse>().Result;
                Assert.IsNotNull(result);
            }
        }

        [TestMethod]
        public void Download_TEST()
        {
            var fileName = "例外1.txt";
            var url = $"api/file/download?fileName={fileName}";

            var response = s_client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            var fileBytes = response.Content.ReadAsByteArrayAsync().Result;
            Assert.IsTrue(fileBytes.Length > 0);
        }
    }
}