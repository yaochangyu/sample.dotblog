using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace WebApiNet48
{
    public class LogFilterAttribute : ActionFilterAttribute
    {
               public LogFilterAttribute()
        {
       
        }

        //public LogFilterAttribute(ITransientMessager transient,
        //                          IScopeMessager     scope,
        //                          ISingleMessager    single)
        //{
        //    this.Transient = transient;
        //    this.Scope     = scope;
        //    this.Single    = single;
        //}

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var requestScope = actionContext.Request.GetDependencyScope();

           var  transient = requestScope.GetService(typeof(ITransientMessager)) as MultiMessager;
            var scope     =requestScope.GetService(typeof(IScopeMessager)) as MultiMessager;
           var  single    = requestScope.GetService(typeof(ISingleMessager)) as MultiMessager;

            var logger = LogManager.GetCurrentClassLogger();
            var content = "我在 LogFilterAttribute.OnActionExecuting\r\n" +
                          $"transient:{transient.OperationId}\r\n" +
                          $"scope:{scope.OperationId}\r\n"         +
                          $"single:{single.OperationId}";
            Console.WriteLine(content);
            logger.Info(content);
            base.OnActionExecuting(actionContext);
        }
    }
}