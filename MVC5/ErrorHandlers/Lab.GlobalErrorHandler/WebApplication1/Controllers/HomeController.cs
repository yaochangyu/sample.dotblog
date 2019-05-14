using System;
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
            var employee = new Employee
            {
                Id   = Guid.NewGuid(),
                Name = "yao",
                Age  = 19
            };
            InstanceUtility.HttpRequestState.SetCurrentVariable(employee);

            throw new Exception("壞掉了");

            return this.View();
        }
    }
}