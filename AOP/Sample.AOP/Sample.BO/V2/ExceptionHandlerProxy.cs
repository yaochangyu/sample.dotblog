using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Sample.AOP.Infrastructure;

namespace Sample.BO.V2
{
    public class ExceptionHandlerProxy<T> : RealProxy
    {
        private readonly T _decorated;

        public ExceptionHandlerProxy(T decorated)
            : base(typeof(T))
        {
            this._decorated = decorated;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            var method = DynamicMethod.BuildMethod(methodInfo);
            this.Log("In Dynamic Proxy - Before executing '{0}'", methodCall.MethodName);
            try
            {
                var result = method.Invoke(this._decorated, methodCall.InArgs);

                this.Log("In Dynamic Proxy - After executing '{0}' ",
                         methodCall.MethodName);
                return new ReturnMessage(result, null, 0,
                                         methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                this.Log(string.Format("In Dynamic Proxy- Exception {0} executing '{1}'", e,
                                       methodCall.MethodName));

                return new ReturnMessage(e, methodCall);
            }
        }

        private void Log(string msg, object arg = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg, arg);
            Console.ResetColor();
        }
    }
}