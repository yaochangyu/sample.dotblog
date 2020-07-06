using System.Web.Mvc;

namespace AspNet48.MVC
{
    public class HomeController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            var key    = typeof(Member).FullName;
            var member = this.HttpContext.Items[key] as Member;
            return this.View(member);
        }
    }
}