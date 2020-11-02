using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using NLog;

namespace WebApiNet48
{
    public class LogFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var requestScope = actionContext.Request.GetDependencyScope();

            var transient = requestScope.GetService(typeof(ITransientMessager)) as MultiMessager;
            var scope     = requestScope.GetService(typeof(IScopeMessager)) as MultiMessager;
            var single    = requestScope.GetService(typeof(ISingleMessager)) as MultiMessager;

            var logger = LogManager.GetCurrentClassLogger();
            var content = "我在 LogFilterAttribute.OnActionExecuting\r\n" +
                          $"transient:{transient.OperationId}\r\n"      +
                          $"scope:{scope.OperationId}\r\n"              +
                          $"single:{single.OperationId}";
            Console.WriteLine(content);
            logger.Info(content);
        }
    }
}