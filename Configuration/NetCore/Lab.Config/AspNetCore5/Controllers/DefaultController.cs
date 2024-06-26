using System;
using Lab.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        [Route("config/appsettings")]
        public IActionResult GetConfig()
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var config          = serviceProvider.GetService<IConfiguration>();
            var content         = new AppSetting();
            config.Bind(content);
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
            appSettingOptions.OnChange(p =>
                                       {
                                           Console.WriteLine("節點已變更");
                                       });
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
    }
}