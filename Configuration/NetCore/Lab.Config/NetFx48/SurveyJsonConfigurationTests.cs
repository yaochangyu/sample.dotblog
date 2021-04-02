using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyJsonConfigurationTests
    {
        [TestMethod]
        public void 切換組態()
        {
            string environmentName;
#if DEBUG
        environmentName = "Development";
#elif QA
                environmentName = "QA";
#elif STAGING
        environmentName = "Staging";
#elif RELEASE
            environmentName = "Production";
#endif
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json",                    false, true)
                                .AddJsonFile($"appsettings.{environmentName}.json", true,  true)
                ;
            var configRoot = configBuilder.Build();

            //讀取組態
            Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
            Console.WriteLine($"Key = {configRoot["Player:Key"]}");
            Console.WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
        }

        [TestMethod]
        public void 手動實例化ConfigurationBuilder()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", true, true)
                ;
            var configRoot = configBuilder.Build();

            //讀取組態

            Console.WriteLine($"AppId = {configRoot["AppId"]}");
            Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
            Console.WriteLine($"Key = {configRoot["Player:Key"]}");
            Console.WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
        }

        [TestMethod]
        public void 注入Configuration()
        {
            var builder = Host.CreateDefaultBuilder(null)
                              .ConfigureAppConfiguration(config =>
                                                         {
                                                             config.Sources.Clear();
                                                             config.AddJsonFile("appsettings.json", true, true);
                                                         })
                              .ConfigureServices(service =>
                                                 {
                                                     //DI  
                                                     service.AddScoped(typeof(AppWorkFlow));
                                                 });
            var host = builder.Build();

            var appService = host.Services.GetService<AppWorkFlow>();
            var playerId   = appService.GetPlayerId();
            Console.WriteLine($"AppId = {playerId}");
        }

        [TestMethod]
        public void 記憶體組態()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddInMemoryCollection(new Dictionary<string, string>
                                {
                                    {"Player:AppId", "player1"},
                                    {"Player:Key", "1234567890"},
                                    {
                                        "ConnectionStrings:DefaultConnectionString",
                                        "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;"
                                    },
                                })
                ;

            var configRoot = configBuilder.Build();

            //讀取組態

            Console.WriteLine($"AppId = {configRoot["AppId"]}");
            Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
            Console.WriteLine($"Key = {configRoot["Player:Key"]}");
            Console.WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
        }

        [TestMethod]
        public void 通過Host()
        {
            using var host = CreateHostBuilder(null).Build();
        }

        [TestMethod]
        public void 實例化JsonConfigurationProvider()
        {
            var configProvider = new JsonConfigurationProvider(new JsonConfigurationSource
            {
                Optional       = false,
                Path           = "appsettings.json",
                ReloadOnChange = true
            });
            configProvider.Load();
            configProvider.TryGet("Player:AppId", out var appId);
            Console.WriteLine($"AppId = {appId}");
        }

        [TestMethod]
        public void 讀取設定檔_GetSection()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var configRoot = builder.Build();

            Console.WriteLine($"AppId = {configRoot.GetSection("AppId")}");
            Console.WriteLine($"AppId = {configRoot.GetSection("Player:AppId")}");
            Console.WriteLine($"Key = {configRoot.GetSection("Player:Key")}");
            Console.WriteLine($"Connection String = {configRoot.GetSection("ConnectionStrings:DefaultConnectionString")}");
        }

        [TestMethod]
        public void 讀取設定檔_GetChild()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var configRoot    = builder.Build();
            var firstSections = configRoot.GetChildren();
            foreach (var firstSection in firstSections)
            {
                var secondSections = firstSection.GetChildren();
                foreach (var secondSection in secondSections)
                {
                    Console.WriteLine($"{secondSection.Key}={secondSection.Value}\tPath={secondSection.Path}");
                }
            }
        }

        [TestMethod]
        public void 讀取設定檔_TryGet()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var configRoot = builder.Build();

            //TryGet
            foreach (var provider in configRoot.Providers)
            {
                provider.TryGet("Player:AppId", out var value);
                Console.WriteLine($"AppId = {value}");
            }
        }

        [TestMethod]
        public void 讀取設定檔_綁定()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var configRoot = builder.Build();

            var appSetting = new AppSetting();
            configRoot.Bind(appSetting);
            Console.WriteLine($"AppId = {appSetting.Player.AppId}");
            Console.WriteLine($"Key = {appSetting.Player.Key}");
            Console.WriteLine($"Connection String = {appSetting.ConnectionStrings.DefaultConnectionString}");
        }

        [TestMethod]
        public void 讀取設定檔_綁定_Get()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var configRoot     = builder.Build();
            var player     = configRoot.GetSection("Player").Get<Player>();
            var appSetting = configRoot.Get<AppSetting>();

            Console.WriteLine($"AppId = {player.AppId}");
            Console.WriteLine($"Key = {appSetting.Player.Key}");
            Console.WriteLine($"Connection String = {appSetting.ConnectionStrings.DefaultConnectionString}");
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureAppConfiguration(config =>
                                                  {
                                                      config.Sources.Clear();
                                                      config.AddJsonFile("appsettings.json", true, true);
                                                      var configRoot = config.Build();

                                                      //讀取組態
                                                      Console.WriteLine($"AppId = {configRoot["AppId"]}");
                                                      Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
                                                      Console.WriteLine($"Key = {configRoot["Player:Key"]}");
                                                      Console
                                                          .WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
                                                  })
                       .ConfigureServices(service =>
                                          {
                                              //DI  
                                          });
        }
    }
}