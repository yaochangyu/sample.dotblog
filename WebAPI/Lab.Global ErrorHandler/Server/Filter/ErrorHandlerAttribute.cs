using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;
using Newtonsoft.Json;
using NLog;

namespace Server.Filter
{
    public class ErrorHandlerAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionContext)
        {
            var exception = actionContext.Exception;
            if (exception == null)
            {
                return;
            }

            var controllerName = actionContext.ActionContext
                                              .ControllerContext
                                              .ControllerDescriptor
                                              .ControllerName;

            var actionName      = actionContext.ActionContext.ActionDescriptor.ActionName;
            var actionArguments = actionContext.ActionContext.ActionArguments;
            var url             = actionContext.Request.RequestUri.ToString();
            var error = new
            {
                Url             = url,
                ControllerName  = controllerName,
                ActionName      = actionName,
                ActionArguments = actionArguments,
            };
            var logger = LogManager.GetLogger($"{controllerName}.{actionName}");

            logger.Error(exception, JsonConvert.SerializeObject(error));
            actionContext.Response = actionContext.Request
                                                  .CreateResponse(HttpStatusCode.InternalServerError, error);

            // 結束 Exception Filter
            throw new HttpResponseException(actionContext.Response);
        }
    }
}