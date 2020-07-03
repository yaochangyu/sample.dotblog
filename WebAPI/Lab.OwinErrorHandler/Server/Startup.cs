using System;
using System.Net;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;

namespace Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            // Web API configuration and services
            config.Services.Replace(typeof(IExceptionHandler), new MyExceptionHandler());

            SwaggerConfig.Register(config);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                                       "DefaultApi",
                                       "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional}
                                      );

            //app.UseErrorPage();

            //app.UseWelcomePage("/Welcome");

            //app.Use((ctx, next) =>
            //        {
            //            var msg = "故意引發例外";
            //            Console.WriteLine(msg);
            //            throw new Exception(msg);
            //        })
            //   .UseWebApi(config);

            app.Use(async (ctx, next) =>
                    {
                        try
                        {
                            await next();
                        }
                        catch (Exception ex)
                        {
                            this.ErrorHandle(ex, ctx);
                        }
                    })
               .Use((ctx, next) =>
                    {
                        var msg = "故意引發例外";
                        Console.WriteLine(msg);
                        throw new Exception(msg);
                    })
               .UseWebApi(config);
        }

        private void ErrorHandle(Exception ex, IOwinContext context)
        {
            //紀錄詳細訊息，完整的例外推疊 ex.ToString()
            context.Response.StatusCode   = (int) HttpStatusCode.InternalServerError;
            context.Response.ReasonPhrase = "Internal Server Error";
            context.Response.ContentType  = "application/json";

            //回應前端，部份訊息，ex.Message
            context.Response.Write(ex.Message);
        }
    }
}