using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NET5.TestProject.File;

namespace NET5.TestProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AutofacController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;

        private readonly ILogger<AutofacController> _logger;

        // public AutofacDefaultController(ILogger<AutofacDefaultController> logger)
        // {
        //     this._logger = logger;
        // }

        public AutofacController(ILogger<AutofacController> logger,
                                        [KeyFilter("zip")] IFileProvider  fileProvider)
        {
            this._logger       = logger;
            this._fileProvider = fileProvider;
            this._fileProvider.Print();
        }

        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok(this._fileProvider.Print());
        }
    }
}