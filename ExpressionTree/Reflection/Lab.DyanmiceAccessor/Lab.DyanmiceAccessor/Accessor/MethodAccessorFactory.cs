using System.Reflection;

namespace Lab.DynamicAccessor
{
    public class MethodAccessorFactory : AccessorFactoryBase<MethodInfo, IMethodAccessor>
    {
        protected override IMethodAccessor Create(MethodInfo key)
        {
            return new MethodAccessor(key);
        }
    }
}