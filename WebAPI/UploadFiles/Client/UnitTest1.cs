using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Console.WriteLine("HttpClient started!");
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp.Dispose();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var url = "/api/upload/upload";
            var fileName = "例外.txt";
            using (var content = new MultipartFormDataContent())
            {
                byte[] imageBytes = File.ReadAllBytes(fileName);
                content.Add(new StreamContent(new MemoryStream(imageBytes)), "File", fileName);
                s_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                var response = s_client.PostAsync(url, content).Result;
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var url = "/api/upload/UploadFormData";
            var fileName = "例外.txt";
            using (var content = new MultipartFormDataContent())
            {
                byte[] imageBytes = File.ReadAllBytes(fileName);
                content.Add(new StreamContent(new MemoryStream(imageBytes)), "File", fileName);
                s_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                var response = s_client.PostAsync(url, content).Result;
            }
        }
    }
}