using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AutofacDefaultController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;

        private readonly ILogger<AutofacDefaultController> _logger;

    
        public AutofacDefaultController(ILogger<AutofacDefaultController> logger,
                                        [KeyFilter("zip")] IFileProvider  fileProvider)
        {
            this._logger       = logger;
            this._fileProvider = fileProvider;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok(this._fileProvider.Print());
        }
    }
}