using System;
using Lab.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCore3.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {
        public IActionResult Get()
        {
            // var setting = this.GetAppSetting();
            var setting = this.GetAppSettingMonitor();
            return this.Ok(setting);
        }

        private AppSetting GetAppSetting()
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var options         = serviceProvider.GetService<IOptions<AppSetting>>();
            return options?.Value;
        }

        private AppSetting GetAppSettingMonitor()
        {
            var serviceProvider    = this.HttpContext.RequestServices;
            var appsSettingOptions = serviceProvider.GetService<IOptionsMonitor<AppSetting>>();
            var playerOption       = serviceProvider.GetService<IOptionsMonitor<Player>>();
            var player1            = playerOption.Get("Player1");
            var player2            = playerOption.Get("Player2");
            Console.WriteLine($"player1={player1.AppId}");
            Console.WriteLine($"player2={player2.AppId}");

            // var appSetting      = options.Get("Player1");
            // return options?.CurrentValue;

            return null;
        }
    }
}