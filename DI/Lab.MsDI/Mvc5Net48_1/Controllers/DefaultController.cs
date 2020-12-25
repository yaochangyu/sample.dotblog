using System;
using System.Web.Mvc;
using Mvc5Net48.Message;

namespace Mvc5Net48.Controllers
{
    public class DefaultController : Controller
    {
        private ITransientMessager Transient { get; }

        private IScopeMessager Scope { get; }

        private ISingleMessager Single { get; }

        public DefaultController(ITransientMessager transient,
                                 IScopeMessager     scope,
                                 ISingleMessager    single)
        {
            this.Transient = transient;
            this.Scope     = scope;
            this.Single    = single;
        }

        // GET: Default
        public ActionResult Index()
        {
            var single    = this.Single;
            var scope     = this.Scope;
            var transient = this.Transient;
            var content = "我在 Controller.Get Action<br>"
                          + $"transient:{transient.OperationId}<br>"
                          + $"scope:{scope.OperationId}<br>"
                          + $"single:{single.OperationId}"
                ;
            Console.WriteLine(content);
            this.ViewBag.Message = content;
            return this.View();
        }
    }
}