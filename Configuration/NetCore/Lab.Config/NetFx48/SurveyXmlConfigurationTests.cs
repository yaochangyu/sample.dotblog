using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyXmlConfigurationTests
    {
        [TestMethod]
        public void 手動實例化ConfigurationBuilder()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddXmlFile("appsettings.xml", false, true);
            var configRoot = configBuilder.Build();

            //讀取組態
            Console.WriteLine($"AppId = {configRoot["AppId"]}");
            Console.WriteLine($"AppId = {configRoot["Player:AppId"]}");
            Console.WriteLine($"Key = {configRoot["Player:Key"]}");
            Console.WriteLine($"Connection String = {configRoot["ConnectionStrings:DefaultConnectionString"]}");
        }
    }
}