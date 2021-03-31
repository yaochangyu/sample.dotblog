using System;
using System.IO;
using Microsoft.Extensions.Configuration;
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

                                                             configBuilder.AddEnvironmentVariables("Custom_");
                                                             var configRoot = configBuilder.Build();

                                                             //讀取組態
                                                             Console
                                                                 .WriteLine($"ASPNETCORE_ENVIRONMENT = {configRoot["ASPNETCORE_ENVIRONMENT"]}");
                                                             Console
                                                                 .WriteLine($"DOTNET_ENVIRONMENT2 = {configRoot["DOTNET_ENVIRONMENT2"]}");
                                                             Console
                                                                 .WriteLine($"CUSTOM_ENVIRONMENT1 = {configRoot["CUSTOM_ENVIRONMENT1"]}");
                                                             Console
                                                                 .WriteLine($"ENVIRONMENT1 = {configRoot["ENVIRONMENT1"]}");
                                                         })
                ;
            builder.Build();
        }

        [TestMethod]
        public void 切換組態設定()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration((hosting, configBuilder) =>
                                                         {
                                                             // config.Sources.Clear();
                                                             var environmentName = hosting.Configuration["ENVIRONMENT2"];
                                                             configBuilder.AddJsonFile("appsettings.json",false,true);
                                                             configBuilder.AddJsonFile($"appsettings.{environmentName}.json",true,true);

                                                             var configRoot = configBuilder.Build();
                                                             
                                                             //讀取組態
                                                             Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
                                                             Console.WriteLine($"Key = {configRoot["Player:Key"]}");
                                                             Console.WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
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
    }
}