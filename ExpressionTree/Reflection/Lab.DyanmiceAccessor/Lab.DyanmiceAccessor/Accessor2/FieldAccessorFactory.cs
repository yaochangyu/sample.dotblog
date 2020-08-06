using System.Reflection;

namespace Lab.DynamicAccessor.Accessor2
{
    public class FieldAccessorFactory : AccessorFactoryBase<FieldInfo, IFieldAccessor>
    {
        protected override IFieldAccessor Create(FieldInfo key)
        {
            return new FieldAccessor(key);
        }
    }
}