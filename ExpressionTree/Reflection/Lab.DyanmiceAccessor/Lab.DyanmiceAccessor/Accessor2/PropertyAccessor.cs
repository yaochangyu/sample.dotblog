using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.DynamicAccessor.Accessor2
{
    public interface IPropertyAccessor
    {
        object GetValue(object instance);

        void SetValue(object instance, object value);
    }

    public class PropertyAccessor : IPropertyAccessor
    {
        public PropertyInfo PropertyInfo { get; }

        private readonly Func<object, object>   _getters;
        private readonly Action<object, object> _setters;

        public PropertyAccessor(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo;
            this._setters     = this.CreateSetter(propertyInfo);
            this._getters     = this.CreateGetter(propertyInfo);
        }

        public object GetValue(object instance)
        {
            throw new NotImplementedException();
        }

        public void SetValue(object instance, object value)
        {
            throw new NotImplementedException();
        }

        internal Func<object, object> CreateGetter(PropertyInfo propertyInfo)
        {
            var              instance   = Expression.Parameter(typeof(object), "instance");
            var              sourceType = propertyInfo.ReflectedType;
            MemberExpression property;
            if (propertyInfo.IsStatic())
            {
                property = Expression.Property(null, propertyInfo);
            }
            else
            {
                var instanceCast = Expression.Convert(instance, sourceType);

                //property = Expression.Property(instanceCast, propertyInfo.Name);

                property = Expression.Property(instanceCast, propertyInfo);
            }

            var propertyCast = Expression.Convert(property, typeof(object));

            var lambda = Expression.Lambda<Func<object, object>>(propertyCast, instance);
            var result = lambda.Compile();

            return result;
        }

        internal Action<object, object> CreateSetter(PropertyInfo propertyInfo)
        {
            var              instance   = Expression.Parameter(typeof(object), "instance");
            var              value      = Expression.Parameter(typeof(object), "value");
            var              sourceType = propertyInfo.DeclaringType;
            MemberExpression property;
            if (propertyInfo.IsStatic())
            {
                property = Expression.Property(null, propertyInfo);
            }
            else
            {
                var instanceCast = Expression.Convert(instance, sourceType);

                //property = Expression.Property(instanceCast, propertyInfo.Name);

                property = Expression.Property(instanceCast, propertyInfo);
            }

            var valueCast = Expression.Convert(value, property.Type);

            var assign = Expression.Assign(property, valueCast);

            //var lambda = Expression.Lambda(typeof(Action<object, object>), assign, instance, value);
            var lambda = Expression.Lambda<Action<object, object>>(assign, instance, value);
            var result = lambda.Compile();

            return result;
        }
    }
}