using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyMemoryConfigurationTests
    {
        [TestMethod]
        public void 讀取記憶體組態()
        {
            var configBuilder = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "Player:AppId", "player1" },
                        { "Player:Key", "1234567890" },
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
        public void 讀取記憶體組態_綁定()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddInMemoryCollection(new Dictionary<string, string>
                                {
                                    { "Player:AppId", "player1" },
                                    { "Player:Key", "1234567890" },
                                    {
                                        "ConnectionStrings:DefaultConnectionString",
                                        "Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;"
                                    },
                                })
                ;

            var configRoot = configBuilder.Build();
            var appSetting = configRoot.Get<AppSetting>();

            //讀取組態

            Console.WriteLine($"AppId = {appSetting.Player.AppId}");
            Console.WriteLine($"Key = {appSetting.Player.Key}");
            Console.WriteLine($"Connection String = {appSetting.ConnectionStrings.DefaultConnectionString}");
        }

        [TestMethod]
        public void 讀取記憶體組態_綁定_集合()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddInMemoryCollection(new Dictionary<string, string>
                                {
                                    { "a:Player:AppId", "player1" },
                                    { "a:Player:Key", "1234567890" },
                                    {
                                        "a:ConnectionStrings:DefaultConnectionString",
                                        "a:Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;"
                                    },
                                    { "b:Player:AppId", "player2" },
                                    { "b:Player:Key", "1234567890" },
                                    {
                                        "b:ConnectionStrings:DefaultConnectionString",
                                        "b:Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;"
                                    },
                                })
                ;

            var configRoot = configBuilder.Build();
            var appSettings = configRoot.Get<IList<AppSetting>>();

            //讀取組態

            Console.WriteLine($"AppId = {appSettings[0].Player.AppId}");
            Console.WriteLine($"Key = {appSettings[0].Player.Key}");
            Console.WriteLine($"Connection String = {appSettings[0].ConnectionStrings.DefaultConnectionString}");
        }

        [TestMethod]
        public void 讀取記憶體組態_綁定_字典()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddInMemoryCollection(new Dictionary<string, string>
                                {
                                    { "a:Player:AppId", "player1" },
                                    { "a:Player:Key", "1234567890" },
                                    {
                                        "a:ConnectionStrings:DefaultConnectionString",
                                        "a:Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;"
                                    },
                                    { "b:Player:AppId", "player2" },
                                    { "b:Player:Key", "1234567890" },
                                    {
                                        "b:ConnectionStrings:DefaultConnectionString",
                                        "b:Server=(localdb)\\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;"
                                    },
                                })
                ;

            var configRoot = configBuilder.Build();
            var appSettings = configRoot.Get<Dictionary<string, AppSetting>>();

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