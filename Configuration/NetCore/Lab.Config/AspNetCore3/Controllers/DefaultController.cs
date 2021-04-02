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
        [Route("options/appsettings")]
        public IActionResult Get()
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var options         = serviceProvider.GetService<IOptions<AppSetting>>();
            return this.Ok(options?.Value);
        }

        [Route("monitor/players/{id}")]
        public IActionResult GetMonitorPlayer(int id)
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var playerOption    = serviceProvider.GetService<IOptionsMonitor<Player>>();
            var player          = playerOption.Get($"Player{id}");
            return this.Ok(player);
        }

        [Route("snapshot/players/{id}")]
        public IActionResult GetSnapshotPlayer(int id)
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var playerOption    = serviceProvider.GetService<IOptionsMonitor<Player>>();
            var player          = playerOption.Get($"Player{id}");
            return this.Ok(player);
        }
    }
}