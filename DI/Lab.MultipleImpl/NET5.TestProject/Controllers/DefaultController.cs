using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {
        private readonly ILogger<DefaultController> _logger;
        private readonly IFileProvider              _fileProvider;

        public DefaultController(ILogger<DefaultController> logger, 
                                 IFileProvider fileProvider)
        {
            this._logger       = logger;
            this._fileProvider = fileProvider;
        }
        [HttpGet]
        public IActionResult Get()
        {
            // var fileProvider = this.HttpContext.RequestServices.GetService<IFileProvider>();
            var fileProvider = this._fileProvider;
            var result       = fileProvider.Print();
            return this.Ok(result);
        }
    }
}