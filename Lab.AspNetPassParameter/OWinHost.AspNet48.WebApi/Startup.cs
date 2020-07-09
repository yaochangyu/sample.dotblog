using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Diagnostics;
using Microsoft.Owin.Diagnostics.Views;
using Microsoft.Owin.Logging;
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
            var logger = app.CreateLogger<ErrorPageMiddleware>();

            app

                //.Use(async (ctx, next) =>
                //     {
                //         try
                //         {
                //             await next();
                //         }
                //         catch (Exception ex)
                //         {
                //             this.Handle(ex, ctx);
                //         }
                //     })
                .Use<CustomErrorPageMiddleware>(new ErrorPageOptions(), logger, true)
                //.UseErrorPage()
                //.Use(async (ctx, next) =>
                //     {
                //         try
                //         {
                //             await next();
                //         }
                //         catch (Exception ex)
                //         {
                //             var owinContext = new OwinContext(ctx.Environment);
                //             this.Handle(owinContext, ex);
                //         }
                //     })
                .Use((ctx, next) =>
                     {
                         var msg = "故意引發例外";
                         Console.WriteLine(msg);
                         throw new Exception(msg);
                     })

                .UseWebApi(config);
        }

          }
}