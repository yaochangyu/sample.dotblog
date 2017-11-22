using System;
using Castle.DynamicProxy;

namespace Sample.BO.V4
{
    public class InjectionFactory
    {
        public static T Create<T>(T instance) where T : class
        {
            var proxyGenerator = new ProxyGenerator();

            var proxy = proxyGenerator.CreateInterfaceProxyWithTarget(instance,
                                                                      ProxyGenerationOptions.Default,
                                                                      new Interceptor());
            //var proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(new Interceptor());
            return proxy;
        }
    }


}