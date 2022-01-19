using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyUserSecretTests
    {
        [TestMethod]
        public void Host讀取秘密()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureHostConfiguration(config =>
                                                          {
                                                              config.AddJsonFile("appsettings.json", false, true);
                                                          })
                ;
            var host = builder.Build();

            var config = host.Services.GetService<IConfiguration>();
            Console.WriteLine($"Player:Key = {config["Player:Key"]}");
            Console.WriteLine($"DbPassword = {config["DbPassword"]}");
        }

        [TestMethod]
        public void 手動實例化組態讀取秘密()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json")
                          .AddUserSecrets<SurveyUserSecretTests>()
                ;

            var config = builder.Build();
            Console.WriteLine($"Player:Key = {config["Player:Key"]}");
        }
    }
}