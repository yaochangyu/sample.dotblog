using System;
using System.Linq.Expressions;
using System.Reflection;
using Lab.DynamicAccessor.TypeConverter;

namespace Lab.DynamicAccessor
{
    public class PropertyAccessor : IPropertyAccessor
    {
        private static readonly TypeConverterFactory s_typeConverterFactory;

        public PropertyInfo PropertyInfo { get; }

        private readonly Func<object, object>   _getter;
        private readonly Action<object, object> _setter;

        static PropertyAccessor()
        {
            s_typeConverterFactory = new TypeConverterFactory();
        }

        public PropertyAccessor(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo;
            this._setter      = this.CreateSetter(propertyInfo);
            this._getter      = this.CreateGetter(propertyInfo);
        }

        public object GetValue(object instance)
        {
            return this._getter(instance);
        }

        public void SetValue(object instance, object value)
        {
            var    propertyType  = this.PropertyInfo.PropertyType;
            var    typeConverter = s_typeConverterFactory.GetTypeConverter(propertyType);
            object targetValue;
            if (propertyType.IsNullableEnum() || propertyType.IsEnum)
            {
                var enumConverter = (EnumConverter) typeConverter;
                targetValue = enumConverter.Convert(propertyType, value);
            }
            else
            {
                targetValue = typeConverter.Convert(value);
            }

            this._setter(instance, targetValue);
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