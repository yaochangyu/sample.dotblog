using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly ITestService            _testService;

        public TestController(ILogger<TestController>             logger,
                              [KeyFilter("service")] ITestService testService)
        {
            this._logger      = logger;
            this._testService = testService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok(this._testService.GetDate());
        }
    }
}