using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Lab.AspNet48.Mvc5.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var url = this.Url.Action("About", "Default");
            return Redirect(url);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}