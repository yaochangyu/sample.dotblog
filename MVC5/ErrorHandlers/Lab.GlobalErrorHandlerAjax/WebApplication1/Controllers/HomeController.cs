using System;
using System.Net;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult About()
        {
            this.ViewBag.Message = "Your application description page.";
            return this.View();
        }

        public ActionResult Contact()
        {
            this.ViewBag.Message = "Your contact page.";
            return this.View();
        }

        public ActionResult Index()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult Query(string id)
        {
            var employee = new Employee
            {
                Id   = Guid.NewGuid(),
                Name = "yao",
                Age  = 19
            };
            InstanceUtility.HttpRequestState.SetCurrentVariable(employee);

            if (string.IsNullOrEmpty(id))
            {
                throw new Exception("壞掉了");
                //return new HttpStatusCodeResult(HttpStatusCode.NoContent, "no content");
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Id not be empty");
                //return new HttpStatusCodeResult(HttpStatusCode.Redirect, "Redirect");
                //return this.HttpNotFound();

                //return this.Json(new
                //                 {
                //                     Id           = id,
                //                     Success      = false,
                //                     ErrorMessage = "Id not be empty"
                //                 },
                //                 JsonRequestBehavior.AllowGet);
            }

            return this.Json(new {Success = true, Id = id}, JsonRequestBehavior.AllowGet);
        }
    }
}