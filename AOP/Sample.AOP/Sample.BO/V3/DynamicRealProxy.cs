using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Sample.AOP.Infrastructure;

namespace Sample.BO.V3
{
    public class DynamicRealProxy<T> : RealProxy
    {
        private readonly T _decorated;

        public DynamicRealProxy(T decorated)
            : base(typeof(T))
        {
            this._decorated = decorated;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;

            var instanceMethod = this.GetMethod(this._decorated, methodInfo);
            try
            {
                var delegateMethod = DynamicMethod.BuildMethod(instanceMethod);
                var authenticationAttribute = instanceMethod.GetCustomAttribute<AuthenticationAttribute>();

                if (authenticationAttribute != null)
                {
                    authenticationAttribute.Validate();
                }

                var result = delegateMethod.Invoke(this._decorated, methodCall.InArgs);

                return new ReturnMessage(result, null, 0,
                                         methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                var exceptionAttribute = instanceMethod.GetCustomAttribute<ExceptionHandlerAttribute>();
                if (exceptionAttribute != null)
                {
                    exceptionAttribute.CatchException(e);
                }

                return new ReturnMessage(e, methodCall);
            }
        }

        private MethodInfo GetMethod(object sourceInstance, MethodInfo targetMethodInfo)
        {
            var targetMethodName = targetMethodInfo.Name;
            var sourceInstanceType = sourceInstance.GetType();
            var sourceInstanceMethods = sourceInstanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            MethodInfo instanceMethod = null;
            foreach (var element in sourceInstanceMethods)
            {
                if (element.Name == targetMethodName)
                {
                    var diff = false;
                    foreach (var sourceParameterInfo in element.GetParameters())
                    {
                        foreach (var targetParameterInfo in targetMethodInfo.GetParameters())
                        {
                            if (sourceParameterInfo.ParameterType != targetParameterInfo.ParameterType)
                            {
                                diff = true;
                            }
                        }
                    }

                    if (!diff)
                    {
                        instanceMethod = element;
                        break;
                    }
                }
            }

            return instanceMethod;
        }
    }
}