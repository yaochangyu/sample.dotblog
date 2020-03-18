using System.Web;
using System.Web.Mvc;

namespace WebApi2.IIS.NET45
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
