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
            var setting = this.GetAppSetting();
            return this.Ok(setting);
        }

        private AppSetting GetAppSetting()
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var options         = serviceProvider.GetService<IOptions<AppSetting>>();
            return options?.Value;
        }
    }
}