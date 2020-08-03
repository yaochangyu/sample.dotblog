using System;
using System.Linq;
using System.Reflection;

namespace Lab.DynamicAccessor
{
    public static class TypeCheckExtensions
    {
        public static bool IsNullableEnum(this Type source)
        {
            var type = Nullable.GetUnderlyingType(source);
            return type != null && type.IsEnum;
        }

        public static bool IsStatic(this PropertyInfo source)
        {
            return source.GetAccessors(true).Any(x => x.IsStatic);
        }
    }
}