using System;
using System.Net.Http;
using System.Web.Http;
using NLog;

namespace WebApiNet48.Controllers
{
    public class Default1Controller : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            var logger       = LogManager.GetCurrentClassLogger();
            var requestScope = this.Request.GetDependencyScope();
            var transient    = requestScope.GetService(typeof(ITransientMessager)) as ITransientMessager;
            var scope        = requestScope.GetService(typeof(IScopeMessager)) as IScopeMessager;
            var single       = requestScope.GetService(typeof(ISingleMessager)) as ISingleMessager;
            var content = "我在 Controller.Get Action\r\n"           +
                          $"transient:{transient.OperationId}\r\n" +
                          $"scope:{scope.OperationId}\r\n"         +
                          $"single:{single.OperationId}";
            Console.WriteLine(content);
            logger.Info(content);
            return this.Ok(content);
        }
    }
}