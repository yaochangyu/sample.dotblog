using System.Web.Mvc;
using NLog;

namespace WebApplication1
{
    public class GlobalHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            var controllerName = filterContext.RouteData.Values["controller"].ToString();
            var actionName     = filterContext.RouteData.Values["action"].ToString();
            var exception      = filterContext.Exception;
            var logger         = LogManager.GetLogger($"{controllerName}.{actionName}");
            var logMessage     = $"例外：{exception.Message}";
            logger.Error(exception, logMessage);

            var handler  = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);
            var viewData = new ViewDataDictionary<HandleErrorInfo>(handler);

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