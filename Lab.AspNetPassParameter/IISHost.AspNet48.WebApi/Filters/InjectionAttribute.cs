using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace AspNet48.WebApi
{
    public class InjectionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            //初始化物件
            var member = new Member {Id = Guid.NewGuid(), Name = Faker.Name.FullName()};
            var key    = member.GetType().FullName;

            //注入到 HttpRequestMessage
            filterContext.Request.Properties[key] = member;
        }
    }
}