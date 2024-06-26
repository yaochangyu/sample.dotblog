using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NET5.TestProject.Controllers;
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
                                     .UseStartup<AutofacStartup>() //<-- add line
                                     .ConfigureServices(services =>
                                                        {
                                                            services.AddAutofac();
                                                            services.AddControllers()
                                                                    .AddControllersAsServices(); //<-- add line
                                                        })
                ;
            using var server = new TestServer(hostBuilder)
            {
                BaseAddress = new Uri("http://localhost:9527")
            };

            var client   = server.CreateClient();
            var url      = "autofac/zip";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

        [TestMethod]
        public void Unity注入ServiceName()
        {
            var unityContainer = new UnityContainer();
            unityContainer.RegisterType<IFileProvider, ZipFileProvider>("zip");
            unityContainer.RegisterType<IFileProvider, FileProvider>("file"); //<-- add line

            var builder = WebHost.CreateDefaultBuilder()
                                 .UseStartup<Startup>()
                                 .UseUnityServiceProvider(unityContainer) //<-- add line
                                 .ConfigureServices(s =>
                                                    {
                                                        s.AddControllers()
                                                         .AddControllersAsServices(); //<-- add line
                                                    })
                ;
            using var server = new TestServer(builder)
            {
                BaseAddress = new Uri("http://localhost:9527")
            };

            var client   = server.CreateClient();
            var url      = "unity/zip";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

        [TestMethod]
        public void 手動註冊()
        {
            var hostBuilder =
                    WebHost.CreateDefaultBuilder()
                           .UseStartup<Startup>()
                           .ConfigureServices(s =>
                                              {
                                                  s.AddSingleton<ZipFileProvider>();
                                                  s.AddSingleton<FileProvider>();
                                                  s.AddSingleton(p =>
                                                                 {
                                                                     var fileProvider = p.GetService<ZipFileProvider>();
                                                                     var logger =
                                                                         p.GetService<ILogger<DefaultController>>();
                                                                     return new DefaultController(logger, fileProvider);
                                                                 });
                                                  s.AddControllers().AddControllersAsServices();
                                              })
                ;
            using var server = new TestServer(hostBuilder)
            {
                BaseAddress = new Uri("http://localhost:9527")
            };

            var client   = server.CreateClient();
            var url      = "default";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

        [TestMethod]
        public void 注入FuncName()
        {
            var builder = WebHost.CreateDefaultBuilder()
                                 .UseStartup<Startup>()
                                 .ConfigureServices(s =>
                                                    {
                                                        s.AddSingleton<ZipFileProvider>();
                                                        s.AddSingleton<FileProvider>();
                                                        s.AddSingleton<Func<string, IFileProvider>>(p =>
                                                            key =>
                                                            {
                                                                switch (key)
                                                                {
                                                                    case "zip":
                                                                        return p
                                                                            .GetService<ZipFileProvider>();
                                                                    case "file":
                                                                        return p
                                                                            .GetService<FileProvider>();
                                                                    default:
                                                                        throw new NotSupportedException();
                                                                }
                                                            });
                                                    })
                ;
            using var server = new TestServer(builder)
            {
                BaseAddress = new Uri("http://localhost:9527")
            };

            var client   = server.CreateClient();
            var url      = "func/zip";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

        [TestMethod]
        public void 注入相同的介面()
        {
            var hostBuilder = WebHost.CreateDefaultBuilder()
                                     .UseStartup<Startup>() //<-- add line
                                     .ConfigureServices(service =>
                                                        {
                                                            ScanToDictionary(service);

                                                            // AddToDictionary(service);
                                                        })
                ;
            using var server = new TestServer(hostBuilder)
            {
                BaseAddress = new Uri("http://localhost:9527")
            };

            var client   = server.CreateClient();
            var url      = "multi/zip";
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var result = response.Content.ReadAsStringAsync().Result;
            Assert.AreEqual("ZipFileProvider", result);
        }

        private static void AddToDictionary(IServiceCollection s)
        {
            s.AddSingleton<ZipFileProvider>();
            s.AddSingleton<FileProvider>();
            s.AddSingleton(p =>
                           {
                               var pool =
                                   new Dictionary<string, IFileProvider>
                                   {
                                       {"zip", p.GetService<ZipFileProvider>()},
                                       {"file", p.GetService<FileProvider>()}
                                   };

                               return pool;
                           });
        }

        private static void ScanToDictionary(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            assembly.GetTypesAssignableFrom<IFileProvider>()
                    .ForEach(t => { services.AddSingleton(t); });
            services.AddSingleton(p =>
                                  {
                                      var pool =
                                          new Dictionary<string, IFileProvider>
                                          {
                                              {"zip", p.GetService<ZipFileProvider>()},
                                              {"file", p.GetService<FileProvider>()}
                                          };

                                      return pool;
                                  });
        }
    }
}