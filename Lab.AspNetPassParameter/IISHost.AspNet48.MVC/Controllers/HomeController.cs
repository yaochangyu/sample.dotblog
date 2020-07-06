using System.Web.Mvc;
using IISHost.AspNet48.MVC.Models;

namespace IISHost.AspNet48.MVC.Controllers
{
    public class HomeController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            var key    = typeof(Member).FullName;
            var member = this.HttpContext.Items[key];
            return this.View(member);
        }
    }
}