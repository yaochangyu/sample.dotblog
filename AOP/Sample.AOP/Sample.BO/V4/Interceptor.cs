using System;
using System.Reflection;
using Castle.DynamicProxy;
using Sample.AOP.Infrastructure;

namespace Sample.BO.V4
{
    public class Interceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            var instanceMethod = invocation.MethodInvocationTarget;
            var instanceArgs = invocation.Arguments;
            var instance = invocation.InvocationTarget;
            try
            {
                var delegateMethod = DynamicMethod.BuildMethod(instanceMethod);
                var authenticationAttribute = instanceMethod.GetCustomAttribute<AuthenticationAttribute>();

                if (authenticationAttribute != null)
                {
                    authenticationAttribute.Validate();
                }

                delegateMethod.Invoke(instance, instanceArgs);
            }
            catch (Exception e)
            {
                var exceptionAttribute = instanceMethod.GetCustomAttribute<ExceptionHandlerAttribute>();
                if (exceptionAttribute != null)
                {
                    exceptionAttribute.CatchException(e);
                }
                throw;
            }
        }
    }
}