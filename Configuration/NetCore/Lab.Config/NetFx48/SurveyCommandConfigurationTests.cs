using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyCommandConfigurationTests
    {
        [TestMethod]
        public void 命令對應()
        {
            string[] args = {"-i=1234567890", "-c=app.json"};

            var map = new Dictionary<string, string>
            {
                {"-i", "--AppId"},
                {"-c", "--Config"}
            };

            var provider = new CommandLineConfigurationProvider(args, map);
            provider.Load();

            provider.TryGet("AppId", out var appId);
            Console.WriteLine($"{args.First()}\r\n" +
                              $"AppId:{appId}");
        }

        [TestMethod]
        public void 傳參數給應用程式_使用Host()
        {
            // string[] args = {"/appId 1234567890"};
            string[] args = {"/AppId=1234567890"};
            var builder = Host.CreateDefaultBuilder(args)
                              .ConfigureAppConfiguration(config =>
                                                         {
                                                             // config.Sources.Clear();
                                                             // config.AddJsonFile("appsettings.json", true, true);
                                                             // config.AddCommandLine(args);
                                                             var configRoot = config.Build();
                                                             Console.WriteLine($"AppId = {configRoot["AppId"]}");
                                                         })
                              .ConfigureServices(service =>
                                                 {
                                                     //DI  
                                                     service.AddScoped(typeof(AppService));
                                                 });
            var host = builder.Build();
        }

        [TestMethod]
        [DataRow(new[] {"--AppId=1234567890"})]
        [DataRow(new[] {"/AppId=1234567890"})]
        [DataRow(new[] {"AppId=1234567890"})]
        public void 實例化CommandLineConfigurationProvider(string[] args)
        {
            var provider = new CommandLineConfigurationProvider(args);
            provider.Load();
            provider.TryGet("AppId", out var appId);
            Console.WriteLine($"{args.First()}\r\n" +
                              $"AppId:{appId}");
        }
    }
}