using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
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
            var config = configBuilder.Build();

            //讀取組態
            Console.WriteLine($"AppId = {config["Player:AppId"]}");
            Console.WriteLine($"Key = {config["Player:Key"]}");
            Console.WriteLine($"Connection String = {config["ConnectionStrings:DefaultConnectionString"]}");
        }

        [TestMethod]
        public void 手動實例化ConfigurationBuilder()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", true, true)
                ;
            var config = configBuilder.Build();

            //讀取組態

            Console.WriteLine($"AppId = {config["AppId"]}");
            Console.WriteLine($"AppId = {config["Player:AppId"]}");
            Console.WriteLine($"Key = {config["Player:Key"]}");
            Console.WriteLine($"Connection String = {config["ConnectionStrings:DefaultConnectionString"]}");
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

            var config = configBuilder.Build();

            //讀取組態

            Console.WriteLine($"AppId = {config["AppId"]}");
            Console.WriteLine($"AppId = {config["Player:AppId"]}");
            Console.WriteLine($"Key = {config["Player:Key"]}");
            Console.WriteLine($"Connection String = {config["ConnectionStrings:DefaultConnectionString"]}");
        }

        [TestMethod]
        public void 通過Host()
        {
            using var host = CreateHostBuilder(null).Build();
        }

        [TestMethod]
        public void 讀取設定檔_GetSection()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var config = builder.Build();

            Console.WriteLine($"AppId = {config.GetSection("AppId")}");
            Console.WriteLine($"AppId = {config.GetSection("Player:AppId")}");
            Console.WriteLine($"Key = {config.GetSection("Player:Key")}");
            Console.WriteLine($"Connection String = {config.GetSection("ConnectionStrings:DefaultConnectionString")}");
        }

        [TestMethod]
        public void 讀取設定檔_TryGet()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var config = builder.Build();

            //TryGet
            foreach (var provider in config.Providers)
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
            var config = builder.Build();

            var appSetting = new AppSetting();
            config.Bind(appSetting);
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
            var config     = builder.Build();
            var player     = config.GetSection("Player").Get<Player>();
            var appSetting = config.Get<AppSetting>();

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