using System;
using System.Reflection;

namespace Lab.DynamicAccessor.Accessor
{
    public class PropertyAccessInfo
    {
        public string PropertyName { get; set; }

        public PropertyInfo PropertyInfo { get; set; }

        public Func<object, object> Getter { get; set; }

        public Action<object, object> Setter { get; set; }
    }
}