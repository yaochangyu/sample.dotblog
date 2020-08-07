using System.Reflection;

namespace Lab.DynamicAccessor
{
    public class PropertyAccessorFactory : AccessorFactoryBase<PropertyInfo, IPropertyAccessor>
    {
        protected override IPropertyAccessor Create(PropertyInfo key)
        {
            return new PropertyAccessor(key);
        }
    }
}
