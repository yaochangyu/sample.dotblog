using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NET5.TestProject.File;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace NET5.TestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Autofac注入ServiceName()
        {
            var hostBuilder = WebHost.CreateDefaultBuilder()
                                     // .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                                     .ConfigureServices(services => { services.AddAutofac(); })
                                     .UseStartup<AutofacStartup>()
                ;
            using var server = new TestServer(hostBuilder)
            {
                BaseAddress = new Uri("http://localhost:9527")
            };

            var client   = server.CreateClient();
            var url      = "autofac";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

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
            var url      = "unity";
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
                                      .UseStartup<FuncStartup>()
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

     

        private static void UseUnityController(IServiceCollection services)
        {
            services.AddControllers()
                    .AddControllersAsServices();
        }
    }
}