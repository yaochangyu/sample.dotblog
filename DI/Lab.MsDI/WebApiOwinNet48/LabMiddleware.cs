using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace WebApiOwinNet48
{
    public class LabMiddleware1 : OwinMiddleware
    {
        private readonly Commander _cmder;

        public LabMiddleware1(OwinMiddleware next, Commander cmder) : base(next)
        {
            this._cmder = this._cmder;
        }

        // public LabMiddleware(OwinMiddleware next) : base(next)
        // {
        // }

        public override Task Invoke(IOwinContext context)
        {
            Console.WriteLine("我在 LabMiddleware.Invoke");

            // Console.WriteLine($"我在 LabMiddleware.Invoke ，Command.Id = {this._cmder.Id}");
            return this.Next.Invoke(context);
        }
    }


    public class LabMiddleware
    {
        private readonly AppFunc _next;

        public LabMiddleware(AppFunc next,Commander cmder)
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