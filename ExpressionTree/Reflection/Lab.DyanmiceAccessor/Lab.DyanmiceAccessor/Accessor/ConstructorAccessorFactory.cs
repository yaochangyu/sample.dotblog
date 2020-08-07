using System.Reflection;

namespace Lab.DynamicAccessor
{
    public class ConstructorAccessorFactory : AccessorFactoryBase<ConstructorInfo, IConstructorAccessor>
    {
        protected override IConstructorAccessor Create(ConstructorInfo key)
        {
            return new ConstructorAccessor(key);
        }
    }
}