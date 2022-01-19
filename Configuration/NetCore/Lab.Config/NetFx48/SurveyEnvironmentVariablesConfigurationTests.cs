using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyEnvironmentVariablesConfigurationTests
    {
        [TestMethod]
        public void Host實例化ConfigurationBuilder()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration((hosting, configBuilder) =>
                              {
                                  // config.Sources.Clear();
                                  var hostingEnvironmentEnvironmentName =
                                      hosting.HostingEnvironment.EnvironmentName;
                                  configBuilder.AddEnvironmentVariables("Custom_");
                                  var configRoot = configBuilder.Build();

                                  //讀取組態
                                  Console
                                      .WriteLine($"ASPNETCORE_ENVIRONMENT = {configRoot["ASPNETCORE_ENVIRONMENT"]}");
                                  Console
                                      .WriteLine($"DOTNET_ENVIRONMENT = {configRoot["DOTNET_ENVIRONMENT"]}");
                                  Console
                                      .WriteLine($"CUSTOM_ENVIRONMENT = {configRoot["CUSTOM_ENVIRONMENT"]}");
                                  Console
                                      .WriteLine($"ENVIRONMENT1 = {configRoot["ENVIRONMENT1"]}");
                              })
                ;
            var host = builder.Build();
            var environment = host.Services.GetRequiredService<IHostEnvironment>();
            Console.WriteLine($"EnvironmentName={environment.EnvironmentName}");
        }

        [TestMethod]
        public void 切換組態設定()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration((hosting, configBuilder) =>
                              {
                                  // config.Sources.Clear();
                                  var environmentName =
                                      hosting.Configuration["ENVIRONMENT2"];
                                  configBuilder.AddJsonFile("appsettings.json", false, true);
                                  configBuilder
                                      .AddJsonFile($"appsettings.{environmentName}.json",
                                                   true, true);

                                  var configRoot = configBuilder.Build();

                                  //讀取組態
                                  Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
                                  Console.WriteLine($"Key = {configRoot["Player:Key"]}");
                                  Console
                                      .WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
                              })
                ;
            builder.Build();
        }

        [TestMethod]
        public void 手動實例化ConfigurationBuilder()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddEnvironmentVariables("ASPNETCORE_")
                ;

            var configRoot = configBuilder.Build();

            //讀取組態
            Console.WriteLine($"ENVIRONMENT = {configRoot["ENVIRONMENT"]}");
        }

        [TestMethod]
        public void 設定主機組態()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureHostConfiguration(config =>
                              {
                                  config.AddJsonFile("appsettings.json", false, true);
                              })
                ;

            var host = builder.Build();
            var environment = host.Services.GetRequiredService<IHostEnvironment>();
            Console.WriteLine($"EnvironmentName={environment.EnvironmentName}");
        }

        [TestMethod]
        public void 讀取環境變數()
        {
            Environment.SetEnvironmentVariable("Player:AppId", "player1");
            Environment.SetEnvironmentVariable("Player:Key", "1234567890");
            Environment.SetEnvironmentVariable("ConnectionStrings:DefaultConnectionString",
                                               "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            var configRoot = configBuilder.Build();

            //讀取組態

            Console.WriteLine($"AppId = {configRoot["AppId"]}");
            Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
            Console.WriteLine($"Key = {configRoot["Player:Key"]}");
            Console.WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
        }
        
        
        [TestMethod]
        public void 讀取環境變數_綁定()
        {
            Environment.SetEnvironmentVariable("Player:AppId", "player1");
            Environment.SetEnvironmentVariable("Player:Key", "1234567890");
            Environment.SetEnvironmentVariable("ConnectionStrings:DefaultConnectionString",
                                               "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            var appSetting = configRoot.Get<AppSetting>();

            //讀取組態

            Console.WriteLine($"AppId = {appSetting.Player.AppId}");
            Console.WriteLine($"Key = {appSetting.Player.Key}");
            Console.WriteLine($"Connection String = {appSetting.ConnectionStrings.DefaultConnectionString}");
        }

        [TestMethod]
        public void 讀取環境變數_綁定_集合()
        {
            Environment.SetEnvironmentVariable("a:Player:AppId", "player1");
            Environment.SetEnvironmentVariable("a:Player:Key", "1234567890");
            Environment.SetEnvironmentVariable("a:ConnectionStrings:DefaultConnectionString",
                                               "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");
            Environment.SetEnvironmentVariable("b:Player:AppId", "player2");
            Environment.SetEnvironmentVariable("b:Player:Key", "1234567890");
            Environment.SetEnvironmentVariable("b:ConnectionStrings:DefaultConnectionString",
                                               "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");

            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            var appSettings = configRoot.Get<IList<AppSetting>>();

            //讀取組態

            Console.WriteLine($"AppId = {appSettings[0].Player.AppId}");
            Console.WriteLine($"Key = {appSettings[0].Player.Key}");
            Console.WriteLine($"Connection String = {appSettings[0].ConnectionStrings.DefaultConnectionString}");
            Console.WriteLine($"AppId = {appSettings[1].Player.AppId}");
            Console.WriteLine($"Key = {appSettings[1].Player.Key}");
            Console.WriteLine($"Connection String = {appSettings[1].ConnectionStrings.DefaultConnectionString}");
        }

        [TestMethod]
        public void 讀取環境變數_綁定_字典()
        {
            Environment.SetEnvironmentVariable("a:Player:AppId", "player1");
            Environment.SetEnvironmentVariable("a:Player:Key", "1234567890");
            Environment.SetEnvironmentVariable("a:ConnectionStrings:DefaultConnectionString",
                                               "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");
            Environment.SetEnvironmentVariable("b:Player:AppId", "player2");
            Environment.SetEnvironmentVariable("b:Player:Key", "1234567890");
            Environment.SetEnvironmentVariable("b:ConnectionStrings:DefaultConnectionString",
                                               "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");

            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            var appSettings = configRoot.Get<Dictionary<string,AppSetting>>();

            //讀取組態

            Console.WriteLine($"AppId = {appSettings["a"].Player.AppId}");
            Console.WriteLine($"Key = {appSettings["a"].Player.Key}");
            Console.WriteLine($"Connection String = {appSettings["a"].ConnectionStrings.DefaultConnectionString}");
            Console.WriteLine($"AppId = {appSettings["b"].Player.AppId}");
            Console.WriteLine($"Key = {appSettings["b"].Player.Key}");
            Console.WriteLine($"Connection String = {appSettings["b"].ConnectionStrings.DefaultConnectionString}");
        }

    }
}