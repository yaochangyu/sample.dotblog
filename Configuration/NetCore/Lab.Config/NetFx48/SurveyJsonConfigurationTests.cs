using System;
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
        public void 手動實例化ConfigurationBuilder()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json");
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

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
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