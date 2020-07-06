using System;
using System.Web.Http;
using Faker;
using Owin;

namespace OWinHost.AspNet48.WebApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional}
                                      );
            app.Use(async (owinContext, next) =>
                    {
                        var member = new Member
                        {
                            Id   = Guid.NewGuid(),
                            Name = Name.FullName()
                        };
                        
                        owinContext.Set(member.GetType().FullName, member);

                        //owinContext.Environment[member.GetType().FullName] = member;
                        await next();
                    });
            app.UseErrorPage()
               .UseWebApi(config);
        }
    }
}