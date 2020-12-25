using System;
using System.Web.Mvc;
using Mvc5Net48.Message;
using NLog;

namespace Mvc5Net48.Controllers
{
    public class Default1Controller : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            var single    = this.Resolver.GetService(typeof(ISingleMessager)) as ISingleMessager;
            var scope     = this.Resolver.GetService(typeof(IScopeMessager)) as IScopeMessager;
            var transient = this.Resolver.GetService(typeof(ITransientMessager)) as ITransientMessager;
            var content = "我在 Controller.Get Action\r\n"
                          + $"transient:{transient.OperationId}\r\n"
                          + $"scope:{scope.OperationId}\r\n"
                          + $"single:{single.OperationId}\r\n"
                ;
            Console.WriteLine(content);
            this.ViewBag.Message = content;
            var logger = LogManager.GetCurrentClassLogger();
            logger.Trace(content);
            return this.View();
        }
    }
}