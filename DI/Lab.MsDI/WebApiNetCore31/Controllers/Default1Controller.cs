using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Default1Controller : ControllerBase
    {
        private readonly ILogger<DefaultController> _logger;

        public Default1Controller(ILogger<DefaultController> logger)
        {
            this._logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var serviceProvider = this.HttpContext.RequestServices;
            var transient       = serviceProvider.GetService(typeof(ITransientMessager)) as ITransientMessager;
            var scope           = serviceProvider.GetService(typeof(IScopeMessager)) as IScopeMessager;
            var single          = serviceProvider.GetService(typeof(ISingleMessager)) as ISingleMessager;
            var content = $"transient:{transient.OperationId}\r\n" +
                          $"scope:{scope.OperationId}\r\n"         +
                          $"single:{single.OperationId}";

            this._logger.LogInformation("transient = {transient},scope = {scope},single = {single}",
                                        transient.OperationId,
                                        scope.OperationId,
                                        single.OperationId);
            return this.Ok(content);
        }
    }
}