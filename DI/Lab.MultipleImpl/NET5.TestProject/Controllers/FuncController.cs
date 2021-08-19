using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FuncController : ControllerBase
    {
        private readonly IFileProvider           _fileProvider;
        private readonly ILogger<FuncController> _logger;

        public FuncController(ILogger<FuncController>     logger,
                              Func<string, IFileProvider> pool)
        {
            this._fileProvider = pool("zip");
            this._logger       = logger;
            var msg = $"{this._fileProvider.Print()} in {this.GetType().Name} constructor";
            Console.WriteLine(msg);
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