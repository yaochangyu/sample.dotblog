using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31
{
    public class LogFilterAttribute : ActionFilterAttribute
    {
        public LogFilterAttribute()
        {
            
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var transient = context.HttpContext.RequestServices.GetService<ITransientMessager>();
            var scope     = context.HttpContext.RequestServices.GetService<IScopeMessager>();
            var single    = context.HttpContext.RequestServices.GetService<ISingleMessager>();
            var logger    = context.HttpContext.RequestServices.GetService<ILogger<LogFilterAttribute>>();
            logger.LogInformation("我在 LogFilterAttribute ,transient = {transient},scope = {scope},single = {single}",
                                  transient.OperationId,
                                  scope.OperationId,
                                  single.OperationId);

            //Console.WriteLine(content);
        }
    }
}