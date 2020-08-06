using System.Reflection;

namespace Lab.DynamicAccessor.Accessor2
{
    public class MemberAccessorManager
    {
        public static AccessorFactoryBase<PropertyInfo, IPropertyAccessor> Property { get; set; }

        static MemberAccessorManager()
        {
            Property = new PropertyAccessorFactory();
        }
    }
}