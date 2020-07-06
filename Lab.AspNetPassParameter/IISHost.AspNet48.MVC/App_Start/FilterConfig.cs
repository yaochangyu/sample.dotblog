using System.Web.Mvc;

namespace AspNet48.MVC
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