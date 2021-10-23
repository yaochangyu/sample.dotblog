using System.Threading;
using Lab.Test.WebApi.Net5.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lab.Test.WebApi.Net5.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly IFileProvider _fileProvider;
        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger,
                              IFileProvider fileProvider)
        {
            this._logger = logger;
            this._fileProvider = fileProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QueryResponse))]
        public IActionResult Get(CancellationToken cancel = default)
        {
            return this.Ok(new QueryResponse
            {
                Message = this._fileProvider.Name()
            });
        }
    }
}