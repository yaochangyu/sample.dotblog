using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetFx48
{
    [TestClass]
    public class SurveyOptionTests
    {
        [TestMethod]
        public void 注入Option()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration((hosting, configBuilder) =>
                                                         {
                                                             // 1.讀組態檔 
                                                             var environmentName =
                                                                 hosting.Configuration["ENVIRONMENT2"];
                                                             configBuilder.AddJsonFile("appsettings.json", false, true);
                                                             configBuilder
                                                                 .AddJsonFile($"appsettings.{environmentName}.json",
                                                                              true, true);
                                                         })
                              .ConfigureServices((hosting, services) =>
                                                 {
                                                     // 2.注入 Options
                                                     services.AddOptions();

                                                     // 3. 注入 IConfiguration
                                                     services.Configure<AppSetting1>(hosting.Configuration);

                                                     //注入其他服務
                                                     services.AddSingleton<AppServiceWithOption>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppServiceWithOption>();
            var playerId = service.GetPlayerId();
            Console.WriteLine($"PlayerId = {playerId}");
        }

        [TestMethod]
        public void 注入OptionMonitor()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration((hosting, configBuilder) =>
                                                         {
                                                             // 1.讀組態檔 
                                                             var environmentName =
                                                                 hosting.Configuration["ENVIRONMENT2"];
                                                             configBuilder.AddJsonFile("appsettings.json", false, true);
                                                             configBuilder
                                                                 .AddJsonFile($"appsettings.{environmentName}.json",
                                                                              true, true);
                                                         })
                              .ConfigureServices((hosting, services) =>
                                                 {
                                                     // 注入 Option 和完整 Configuration
                                                     services.Configure<AppSetting1>(hosting.Configuration);

                                                     // 注入 Option 和特定 Configuration Section Name
                                                     services.Configure<Player1>("Player",
                                                         hosting.Configuration.GetSection("Player"));

                                                     //注入其他服務
                                                     services.AddScoped<AppServiceWithOptionsMonitor>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppServiceWithOptionsMonitor>();
            var playerId = service.GetPlayerId();
            Console.WriteLine($"PlayerId = {playerId}");
        }

        [TestMethod]
        public void 注入OptionSnapshot()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureAppConfiguration((hosting, configBuilder) =>
                                                         {
                                                             var environmentName =
                                                                 hosting.Configuration["ENVIRONMENT2"];
                                                             configBuilder.AddJsonFile("appsettings.json", false, true);
                                                             configBuilder
                                                                 .AddJsonFile($"appsettings.{environmentName}.json",
                                                                              true, true);
                                                         })
                              .ConfigureServices((hosting, services) =>
                                                 {
                                                     //  注入 Option by 完整組態
                                                     services.Configure<AppSetting1>(hosting.Configuration);

                                                     // 注入 Option by 特定組態
                                                     services.Configure<Player1>(hosting.Configuration
                                                         .GetSection("Player"));

                                                     //注入其他服務
                                                     services.AddScoped<AppServiceWithOptionsSnapshot>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppServiceWithOptionsSnapshot>();
            var playerId = service.GetPlayerId();
            Console.WriteLine($"PlayerId = {playerId}");
        }
    }
}