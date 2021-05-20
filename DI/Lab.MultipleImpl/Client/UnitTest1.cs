using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Server;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace Client
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Unity注入ServiceName()
        {
            var unityContainer = new UnityContainer();
            ConfigureContainer(unityContainer);

            using var server =
                new TestServer(WebHost.CreateDefaultBuilder()
                                      .UseStartup<Startup>()
                                      .UseUnityServiceProvider(unityContainer)
                                      .ConfigureServices(UseUnityController)
                              )
                {
                    BaseAddress = new Uri("http://localhost:9527")
                };

            var client   = server.CreateClient();
            var url      = "UnityDefault";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

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
            Assert.AreEqual("ZipFileProvider", result);
        }

        private static void ConfigureContainer(IUnityContainer container)
        {
            container.RegisterType<IFileProvider, ZipFileProvider>("zip");
            container.RegisterType<IFileProvider, FileProvider>("file");
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

        private static void UseUnityController(IServiceCollection services)
        {
            services.AddControllers()
                    .AddControllersAsServices();
        }
    }
}