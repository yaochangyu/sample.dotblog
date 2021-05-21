using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UnityController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;

        private readonly ILogger<UnityController> _logger;

        public UnityController(ILogger<UnityController>          logger,
                               [Dependency("zip")] IFileProvider fileProvider)
        {
            this._logger       = logger;
            this._fileProvider = fileProvider;
        }

        [HttpGet]
        [Route("{key}")]
        public IActionResult Get(string key)
        {
            var serviceProvider      = this.HttpContext.RequestServices;
            var unityServiceProvider = (ServiceProvider) serviceProvider;
            var unityContainer       = (UnityContainer) unityServiceProvider;
            var fileProvider         = unityContainer.Resolve<IFileProvider>(key);
            var result               = fileProvider.Print();
            return this.Ok(result);
        }
    }
}