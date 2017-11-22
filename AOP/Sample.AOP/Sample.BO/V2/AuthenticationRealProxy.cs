using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Sample.AOP.Infrastructure;

namespace Sample.BO.V2
{
    public class AuthenticationRealProxy<T> : RealProxy
    {
        private readonly T _decorated;

        public AuthenticationRealProxy(T decorated)
            : base(typeof(T))
        {
            this._decorated = decorated;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            var method = DynamicMethod.BuildMethod(methodInfo);
            var principal = Thread.CurrentPrincipal;
            if (!principal.Identity.IsAuthenticated)
            {
                throw new Exception("沒有通過驗證");
            }

            if (!principal.IsInRole("Admin"))
            {
                throw new Exception("沒有在Admin群裡");
            }

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