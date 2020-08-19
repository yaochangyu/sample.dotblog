using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Server
{
    public class ErrorHandlerOwinMiddleware : OwinMiddleware
    {
        public ErrorHandlerOwinMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                await this.Next.Invoke(context);
            }
            catch (Exception ex)
            {
                try
                {
                    this.Handle(ex, context);
                    return;
                }
                catch (Exception)
                {
                    // If there's a Exception while generating the error page, re-throw the original exception.
                }

                throw;
            }
        }

        private void Handle(Exception ex, IOwinContext context)
        {
            //Build a model to represet the error for the client
            context.Response.StatusCode   = (int) HttpStatusCode.InternalServerError;
            context.Response.ReasonPhrase = "Internal Server Error";
            context.Response.ContentType  = "application/json";
            context.Response.Write(JsonConvert.SerializeObject(ex));
        }
    }
}