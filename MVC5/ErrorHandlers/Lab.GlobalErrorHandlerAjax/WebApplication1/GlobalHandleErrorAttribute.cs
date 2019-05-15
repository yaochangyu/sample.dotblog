using System.Net;
using System.Web.Mvc;
using NLog;

namespace WebApplication1
{
    public class GlobalHandleErrorAttribute : HandleErrorAttribute
    {
        private static void SetContentResult(ExceptionContext filterContext, object request)
        {
            var content = filterContext.HttpContext.IsDebuggingEnabled
                              ? filterContext.Exception.StackTrace
                              : "An unexpected error has occurred. Please contact the system administrator.";

            filterContext.Result = new ContentResult
            {
                ContentType = "text/plain",
                Content     = content
            };

            filterContext.HttpContext.Response.Status = "500 " +
                                                        filterContext.Exception.Message
                                                                     .Replace("\r", " ")
                                                                     .Replace("\n", " ");
        }

        private static void SetJsonResult(ExceptionContext filterContext, object request)
        {
            dynamic data;
            if (filterContext.HttpContext.IsDebuggingEnabled)
            {
                data = new
                {
                    filterContext.Exception.Message,
                    filterContext.Exception.StackTrace,
                    RequestVariable = request
                };
            }
            else
            {
                data = new
                {
                    Message = "An unexpected error has occurred. Please contact the system administrator."
                };
            }

            filterContext.Result = new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data                = data
            };
        }

        public override void OnException(ExceptionContext filterContext)
        {
            var controllerName = filterContext.RouteData.Values["controller"].ToString();
            var actionName     = filterContext.RouteData.Values["action"].ToString();
            var exception      = filterContext.Exception;
            var logger         = LogManager.GetLogger($"{controllerName}.{actionName}");
            var requestJson    = InstanceUtility.HttpRequestState.GetCurrentVariableToJson();
            var request        = InstanceUtility.HttpRequestState.GetCurrentVariable();
            var logMessage     = $"例外：{exception.Message},\r\n傳入參數:{requestJson}";
            logger.Error(exception, logMessage);

            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode             = (int) HttpStatusCode.InternalServerError;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;

            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                SetJsonResult(filterContext, request);
                //SetContentResult(filterContext, request);
            }
            else
            {
                var handler  = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);
                var viewData = new ViewDataDictionary<HandleErrorInfo>(handler);

                viewData.Add("Request_Variable", request); //傳入自訂的ViewData

                //var viewName = "/Views/Error/Error.cshtml";
                filterContext.Result = new ViewResult
                {
                    ViewName   = this.View,
                    MasterName = this.Master,
                    ViewData   = viewData,
                    TempData   = filterContext.Controller.TempData
                };
            }
        }
    }
}