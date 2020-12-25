using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Mvc5Net48.Message;

namespace Mvc5Net48.Controllers
{
    public class DefaultController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            var single = this.Resolver.GetService(typeof(ISingleMessager)) as ISingleMessager;
            var scope = this.Resolver.GetService(typeof(IScopeMessager)) as IScopeMessager;
            var transient = this.Resolver.GetService(typeof(ITransientMessager)) as ITransientMessager;
            var content = "我在 Controller.Get Action<br>"
                          + $"transient:{transient.OperationId}<br>" 
                          + $"scope:{scope.OperationId}<br>" 
                          + $"single:{single.OperationId}" 
                          ;
            Console.WriteLine(content);
            this.ViewBag.Message = content;
            return View();
        }
    }
}