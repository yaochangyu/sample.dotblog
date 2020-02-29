using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Newtonsoft.Json;

namespace Lab.HangfireManager.AspNet48
{
    /// <summary>
    ///     unexpected error handler override.
    /// </summary>
    public class ErrorHandlerAttribute : ExceptionFilterAttribute

    {
        /// <summary>
        ///     override exception method.
        ///     Global error handler: 500 internal server error(excpeted & unexpected)
        /// </summary>
        /// <param name="actionContext"></param>
        public override void OnException(HttpActionExecutedContext actionContext)
        {
            var controllerName = actionContext.ActionContext
                                              .ControllerContext
                                              .ControllerDescriptor
                                              .ControllerName;
            var actionName = actionContext.ActionContext.ActionDescriptor.ActionName;
            var exception  = actionContext.Exception;

            //var requestVariable = actionContext.Request.GetRequestVariable();
            //var requestJson     = requestVariable == null ? "Empty" : JsonConvert.SerializeObject(requestVariable);
            //var logger          = LogManager.GetLogger($"{controllerName}.{actionName}");

            ////var name = actionContext.Request.GetRequestContext().Principal.Identity.Name; // HttpContext.Current.User.Identity.Name;
            //var logMessage = $"{exception.Message}.\r\n傳入參數->{requestJson}";
            //logger.Error(exception, logMessage);
            var json = JsonConvert.SerializeObject(new {Message = $"{exception.Message}"});
            actionContext.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(json)
            };

            //base.OnException(actionContext);
        }
    }
}