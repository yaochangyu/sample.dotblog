using System;
using System.Diagnostics;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mvc5Net48_1.Message;
using NLog;

namespace Mvc5Net48_1
{
    public class LogFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var serviceScope = filterContext.HttpContext?.Items[typeof(IServiceScope)] as IServiceScope;
            if (serviceScope == null)
            {
                return;
            }

            var serviceProvider = serviceScope.ServiceProvider;

            var transient = serviceProvider.GetService<ITransientMessager>();
            var single    = serviceProvider.GetService<ISingleMessager>();

            var scope     = serviceProvider.GetService<IScopeMessager>();
            var scope2    = DependencyResolver.Current.GetService<IScopeMessager>();
            var noeq = scope.OperationId == scope2.OperationId;
            Debug.Assert(noeq);

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