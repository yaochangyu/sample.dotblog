using System;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mvc5Net48.Message;

namespace WebApiNet48
{
    public class LogFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            
            var requiredService = filterContext.HttpContext.GetRequiredService(typeof(ITransientMessager));
            var transient       = httpContext.GetService<ITransientMessager>();
            var scope           = httpContext.GetService<IScopeMessager>();
            var single          = httpContext.GetService<ISingleMessager>();

            //var logger = LogManager.GetCurrentClassLogger();
            var content = "我在 LogFilterAttribute.OnActionExecuting\r\n" +
                          $"transient:{transient.OperationId}\r\n"      +
                          $"scope:{scope.OperationId}\r\n"              +
                          $"single:{single.OperationId}";
            Console.WriteLine(content);

            //logger.Info(content);
        }
    }
}