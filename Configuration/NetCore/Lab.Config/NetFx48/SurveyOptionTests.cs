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
                                                     // 2. 注入 Option 和 Configuration
                                                     services.Configure<AppSetting1>(hosting.Configuration);

                                                     //注入其他服務
                                                     services.AddSingleton<AppWorkFlowWithOption>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppWorkFlowWithOption>();
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
                                                     services.AddScoped<AppWorkFlowWithOptionsMonitor>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppWorkFlowWithOptionsMonitor>();
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
                                                     services.AddScoped<AppWorkFlowWithOptionsSnapshot>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppWorkFlowWithOptionsSnapshot>();
            var playerId = service.GetPlayerId();
            Console.WriteLine($"PlayerId = {playerId}");
        }

[TestMethod]
        public void 驗證()
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
                                                     // 2. 注入 Option 和 Configuration
                                                     services.Configure<AppSetting1>(hosting.Configuration);
                                                     //驗證
                                                     services.AddOptions<AppSetting1>()
                                                             .ValidateDataAnnotations()
                                                             .Validate(p =>
                                                                       {
                                                                           var hasContent = string.IsNullOrWhiteSpace(p.ConnectionStrings.DefaultConnectionString);
                                                                           if (hasContent == false)
                                                                           {
                                                                               return false;
                                                                           }

                                                                           return true;
                                                                       },
                                                                       "DefaultConnectionString must be value"); // Failure message.
                                                     ;

                                                     //注入其他服務
                                                     services.AddSingleton<AppWorkFlowWithOption>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppWorkFlowWithOption>();
            var playerId = service.GetPlayerId();
            Console.WriteLine($"PlayerId = {playerId}");
        }
       
        [TestMethod]
        public void 直接注入組態物件()
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
                                                     var appSetting = hosting.Configuration.Get<AppSetting>();
                                                     services.AddSingleton(typeof(AppSetting), appSetting);
                                                     
                                                     //注入其他服務
                                                     services.AddSingleton<AppWorkFlow1>();
                                                 })
                ;
            var host     = builder.Build();
            var service  = host.Services.GetService<AppWorkFlow1>();
            var playerId = service.GetPlayerId();
            Console.WriteLine($"PlayerId = {playerId}");
        }
        
    }
}