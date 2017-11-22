using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Sample.AOP.Infrastructure;

namespace Sample.BO.V2
{
    public class ExceptionHandlerRealProxy<T> : RealProxy
    {
        private readonly T _decorated;

        public ExceptionHandlerRealProxy(T decorated)
            : base(typeof(T))
        {
            this._decorated = decorated;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            var method = DynamicMethod.BuildMethod(methodInfo);

            try
            {
                var result = method.Invoke(this._decorated, methodCall.InArgs);
                return new ReturnMessage(result, null, 0,
                                         methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                return new ReturnMessage(e, methodCall);
            }
        }
    }
}