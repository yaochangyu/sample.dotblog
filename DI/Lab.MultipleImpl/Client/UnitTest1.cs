using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Server;

namespace Client
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void 注入FuncName()
        {
            using var server =
                new TestServer(WebHost.CreateDefaultBuilder()
                                      .UseStartup<Startup>()
                                      .ConfigureServices(UseFuncName)
                              )
                {
                    BaseAddress = new Uri("http://localhost:9527")
                };

            var client   = server.CreateClient();
            var url      = "default/zip";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider",result);
        }

        private static void UseFuncName(IServiceCollection services)
        {
            services.AddSingleton<ZipFileProvider>();
            services.AddSingleton<FileProvider>();
            services.AddSingleton<Func<string, IFileProvider>>(provider =>
                                                                   key =>
                                                                   {
                                                                       switch (key)
                                                                       {
                                                                           case "zip":
                                                                               return provider
                                                                                   .GetService<ZipFileProvider>();
                                                                           case "file":
                                                                               return provider
                                                                                   .GetService<FileProvider>();
                                                                           default:
                                                                               throw new NotSupportedException();
                                                                       }
                                                                   });
        }
    }
}