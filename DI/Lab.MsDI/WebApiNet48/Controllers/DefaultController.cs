using System;
using System.Web.Http;
using NLog;

namespace WebApiNet48.Controllers
{
    public class DefaultController : ApiController
    {
        private IMessager Transient { get; }

        private IMessager Scope { get; }

        private IMessager Single { get; }

        public DefaultController(ITransientMessager transient,
                                 IScopeMessager     scope,
                                 ISingleMessager    single)
        {
            this.Transient = transient;
            this.Scope     = scope;
            this.Single    = single;
        }

        [HttpGet]
        public IHttpActionResult Get()
        {
            var logger = LogManager.GetCurrentClassLogger();

            var content = "我在 Controller.Get Action\r\n"                +
                          $"transient:{this.Transient.OperationId}\r\n" +
                          $"scope:{this.Scope.OperationId}\r\n"         +
                          $"single:{this.Single.OperationId}";
            Console.WriteLine(content);
            logger.Info(content);

            //this._logger.LogInformation("transient = {transient},scope = {scope},single = {single}",
            //                            this.Transient.OperationId,
            //                            this.Scope.OperationId,
            //                            this.Single.OperationId);
            return this.Ok(content);
        }

        
      }
}