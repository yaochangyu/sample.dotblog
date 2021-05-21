using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;
using Unity;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UnityController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;

        private readonly ILogger<UnityController> _logger;

        public UnityController(ILogger<UnityController>   logger,
                                      [Dependency("zip")] IFileProvider fileProvider)
        {
            this._logger       = logger;
            this._fileProvider = fileProvider;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var serviceProvider      = this.HttpContext.RequestServices;
            var unityServiceProvider = (Unity.Microsoft.DependencyInjection.ServiceProvider) serviceProvider;
            var unityContainer       = (UnityContainer) unityServiceProvider;
            var fileProvider         = unityContainer.Resolve<IFileProvider>("zip");
            var result               = fileProvider.Print();
            return this.Ok(result);
        }
    }
}