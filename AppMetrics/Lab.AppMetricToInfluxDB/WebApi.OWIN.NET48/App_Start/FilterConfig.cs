using System.Web;
using System.Web.Mvc;

namespace WebApi.OWIN.NET48
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
