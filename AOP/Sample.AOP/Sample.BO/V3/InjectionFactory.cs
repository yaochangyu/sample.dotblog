using System;

namespace Sample.BO.V3
{
    public class InjectionFactory
    {
        public static T Create<T>(T instance)
        {
            var proxy = new DynamicRealProxy<T>(instance);
            return (T) proxy.GetTransparentProxy();
        }
    }
}