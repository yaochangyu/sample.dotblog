using System.Threading;
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
        public IActionResult Get(CancellationToken cancel)
        {
            return this.Ok(new QueryResponse
            {
                Message = "Hello"
            });
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QueryResponse))]
        [Route("file")]
        public IActionResult GetFile(CancellationToken cancel)
        {
            return this.Ok(new QueryResponse
            {
                Message = this._fileProvider.Name()
            });
        }
    }

    public class QueryResponse
    {
        public string Message { get; set; }
    }
}