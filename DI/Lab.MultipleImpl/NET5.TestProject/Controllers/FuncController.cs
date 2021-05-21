using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FuncController : ControllerBase
    {
        private readonly ILogger<FuncController> _logger;

        public FuncController(ILogger<FuncController> logger)
        {
            this._logger = logger;
        }

        [HttpGet]
        [Route("{type}")]
        public IActionResult Get(string type)
        {
            var fileProvider = this.HttpContext.RequestServices.GetService<IFileProvider>(type);
            var result       = fileProvider.Print();
            return this.Ok(result);
        }
    }
}