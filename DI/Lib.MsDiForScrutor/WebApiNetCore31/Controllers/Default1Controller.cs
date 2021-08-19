using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Default1Controller : ControllerBase
    {
        private IMessager Messager { get; }

        private readonly ILogger<Default1Controller> _logger;
        //public Default1Controller(IMessager messager)
        //{
        //    this.Messager = messager;
        //}

        public Default1Controller(ILogger<Default1Controller> logger,
                                  IMessager                   messager
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