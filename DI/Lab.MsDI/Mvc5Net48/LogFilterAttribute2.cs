using System;
using System.Diagnostics;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mvc5Net48.Message;
using NLog;

namespace Mvc5Net48
{
    public class LogFilterAttribute2 : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // var transientMessager = filterContext.HttpContext.GetService<ITransientMessager>();//失敗

            var transient         = DependencyResolver.Current.GetService<ITransientMessager>();
            var single            = DependencyResolver.Current.GetService<ISingleMessager>();
            var scope             = DependencyResolver.Current.GetService<IScopeMessager>();

            var logger = LogManager.GetCurrentClassLogger();
            var content = "我在 LogFilterAttribute.OnActionExecuting\r\n" +
                          $"transient:{transient.OperationId}\r\n"      +
                          $"scope:{scope.OperationId}\r\n"              +
                          $"single:{single.OperationId}";
            Console.WriteLine(content);

            logger.Trace(content);
        }
    }
}