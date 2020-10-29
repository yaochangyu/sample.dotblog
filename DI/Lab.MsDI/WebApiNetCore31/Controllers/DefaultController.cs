using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DefaultController : ControllerBase
    {
        private IMessager Transient { get; }

        private IMessager Scope { get; }

        private IMessager Single { get; }

        private readonly ILogger<DefaultController> _logger;

        public DefaultController(ILogger<DefaultController> logger,
                                 ITransientMessager         transient,
                                 IScopeMessager             scope,
                                 ISingleMessager            single)
        {
            this._logger = logger;

            this.Transient = transient;
            this.Scope     = scope;
            this.Single    = single;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var content = $"transient:{this.Transient.OperationId}\r\n" +
                          $"scope:{this.Scope.OperationId}\r\n"         +
                          $"single:{this.Single.OperationId}";
            this._logger.LogInformation("transient = {transient},scope = {scope},single = {single}",
                                        this.Transient.OperationId,
                                        this.Scope.OperationId,
                                        this.Single.OperationId);
            return this.Ok(content);
        }
    }
}