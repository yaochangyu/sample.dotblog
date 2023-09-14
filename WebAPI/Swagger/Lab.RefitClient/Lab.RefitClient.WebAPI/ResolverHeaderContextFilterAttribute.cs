using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lab.RefitClient.WebAPI
{
    public class ResolverHeaderContextFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var idempotencyKey = actionContext.HttpContext.Request.Headers[PetStoreHeaderNames.IdempotencyKey];
            var apiKey = actionContext.HttpContext.Request.Headers[PetStoreHeaderNames.ApiKey];
            var headerContext = actionContext.HttpContext.RequestServices.GetService<IContextSetter<HeaderContext>>();
            headerContext.Set(new HeaderContext
            {
                IdempotencyKey = idempotencyKey,
                ApiKey = apiKey
            });
        }
    }
}