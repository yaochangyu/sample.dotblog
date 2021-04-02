using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
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
                {"-i", "AppId"},
                {"-c", "Config"}
            };
        
            var provider = new CommandLineConfigurationProvider(args, map);
            provider.Load();
        
            provider.TryGet("AppId", out var appId);
            provider.TryGet("Config", out var configPath);
            Console.WriteLine($"{args.First()}\r\n" +
                              $"AppId:{appId}\r\n"  +
                              $"ConfigPath:{configPath}");
        }

        [TestMethod]
        [DataRow(new[] {"-i=1234567890", "-c=app.json"})]
        public void 命令對應_Host(string[] args)
        {
            var map = new Dictionary<string, string>
            {
                {"-i", "AppId"},
                {"-c", "Config"}
            };
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration(config =>
                                                         {
                                                             // config.Sources.Clear();
                                                             config.AddCommandLine(args, map);
                                                             var configRoot = config.Build();

                                                             var appId      = configRoot["AppId"];
                                                             var configPath = configRoot["Config"];
                                                             Console.WriteLine($"{args.First()}\r\n" +
                                                                               $"AppId:{appId}\r\n"  +
                                                                               $"ConfigPath:{configPath}");
                                                         })
                              .ConfigureServices(service =>
                                                 {
                                                     //DI  
                                                     service.AddScoped(typeof(AppWorkFlow));
                                                 })
                ;
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