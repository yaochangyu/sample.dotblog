using System;
using Lab.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCore5.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        [Route("options/appsettings")]
        public IActionResult Get()
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var content         = serviceProvider.GetService<IOptions<AppSetting>>()?.Value;
            return this.Ok(content);
        }

        [HttpGet]
        [Route("monitor/players/{id}")]
        public IActionResult GetMonitorPlayer(int id)
        {
            var serviceProvider   = this.HttpContext.RequestServices;
            var appSettingOptions = serviceProvider.GetService<IOptionsMonitor<AppSetting>>();
            var playerOptions     = serviceProvider.GetService<IOptionsMonitor<Player>>();
            var content = new
            {
                App    = appSettingOptions?.CurrentValue,
                Player = playerOptions?.Get($"Player{id}")
            };
            return this.Ok(content);
        }

        [HttpGet]
        [Route("snapshot/players/{id}")]
        public IActionResult GetSnapshotPlayer(int id)
        {
            var serviceProvider   = this.HttpContext.RequestServices;
            var appSettingOptions = serviceProvider.GetService<IOptionsSnapshot<AppSetting>>();
            var playerOptions     = serviceProvider.GetService<IOptionsSnapshot<Player>>();
            var content = new
            {
                App    = appSettingOptions?.Value,
                Player = playerOptions?.Get($"Player{id}")
            };
            return this.Ok(content);
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
            return appsSettingOptions?.CurrentValue;
        }
    }
}