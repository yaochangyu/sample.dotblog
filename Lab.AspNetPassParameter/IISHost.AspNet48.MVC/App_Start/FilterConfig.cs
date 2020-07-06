using System.Web.Mvc;
using IISHost.AspNet48.MVC.Filters;

namespace IISHost.AspNet48.MVC
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new InjectionAttribute());
        }
    }
}
