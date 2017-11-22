using System;

namespace Sample.BO.V3
{
    [AttributeUsage(AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class ExceptionHandlerAttribute : Attribute
    {
        public void CatchException(Exception e)
        {
            Console.WriteLine($"{e.Message},{e.StackTrace}");
        }

    }
}