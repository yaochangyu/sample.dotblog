using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.RefitClient.WebAPI
{
    public class GenHeaderContextFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var key = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            var headerContext = actionContext.HttpContext.RequestServices.GetService<IContextSetter<HeaderContext>>();
            headerContext.Set(new HeaderContext
            {
                IdempotencyKey = key,
                ApiKey = key
            });
        }
    }
}