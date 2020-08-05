using System;
using System.Reflection;

namespace Lab.DynamicAccessor
{
    public class PropertyAccessInfo
    {
        public string PropertyName { get; set; }

        public PropertyInfo PropertyInfo { get; set; }

        public Func<object, object> Getter { get; set; }

        public Action<object, object> Setter { get; set; }

        public object GetValue(object instance)
        {
            return this.Getter(instance);
        }

        public void SetValue(object instance, object value)
        {
            this.Setter(instance, value);
        }
    }
}