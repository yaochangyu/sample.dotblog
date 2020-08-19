using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Server
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ErrorHandler
    {
        private readonly AppFunc _next;

        public ErrorHandler(AppFunc next)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            this._next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            try
            {
                await this._next(environment);
            }
            catch (Exception ex)
            {
                var owinContext = new OwinContext(environment);

                this.ErrorHandle(ex, owinContext);
            }
        }

        private void ErrorHandle(Exception ex, IOwinContext context)
        {
            context.Response.StatusCode   = (int) HttpStatusCode.InternalServerError;
            context.Response.ReasonPhrase = "Internal Server Error";
            context.Response.ContentType  = "application/json";
            context.Response.Write(ex.Message);
        }
    }
}