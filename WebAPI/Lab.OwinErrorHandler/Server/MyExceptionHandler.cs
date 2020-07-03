using System;
using System.Web.Http.ExceptionHandling;

namespace Server
{
    public class MyExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            Console.WriteLine(context.Exception.Message);
        }
    }
}