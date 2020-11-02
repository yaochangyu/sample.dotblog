using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {
        private IMessager Messager { get; }

        private readonly ILogger<DefaultController> _logger;

        public DefaultController(ILogger<DefaultController> logger,
                                 IMessager                  messager
        )
        {
            this._logger  = logger;
            this.Messager = messager;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var content = $"Messager:{this.Messager.OperationId}";
            this._logger.LogInformation("Messager:{message}", content);
            return this.Ok(content);
        }
    }
}