using System.Web.Http;

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
            var content = $"transient:{this.Transient.OperationId}\r\n" +
                          $"scope:{this.Scope.OperationId}\r\n"         +
                          $"single:{this.Single.OperationId}";

            //this._logger.LogInformation("transient = {transient},scope = {scope},single = {single}",
            //                            this.Transient.OperationId,
            //                            this.Scope.OperationId,
            //                            this.Single.OperationId);
            return this.Ok(content);
        }
    }
}