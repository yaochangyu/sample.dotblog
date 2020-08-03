using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Lab.DynamicAccessor.TypeConverter;

namespace Lab.DynamicAccessor
{
    public interface IPropertyAccessor
    {
        object GetValue(object instance, PropertyInfo propertyInfo);

        void SetValue(object instance, PropertyInfo propertyInfo, object value);
    }

    public class PropertyAccessor : IPropertyAccessor
    {
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> s_setters;
        private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>>   s_getters;
        private static readonly TypeConverterFactory                                       s_typeConverterFactory;

        static PropertyAccessor()
        {
            s_setters              = new ConcurrentDictionary<PropertyInfo, Action<object, object>>();
            s_getters              = new ConcurrentDictionary<PropertyInfo, Func<object, object>>();
            s_typeConverterFactory = new TypeConverterFactory();
        }

        public virtual object GetValue(object instance, PropertyInfo propertyInfo)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var getter = GetOrCreateGetter(instance.GetType(), propertyInfo);
            return getter(instance);
        }

        public virtual void SetValue(object instance, PropertyInfo propertyInfo, object newValue)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var    propertyType  = propertyInfo.PropertyType;
            var    typeConverter = s_typeConverterFactory.GetTypeConverter(propertyType);
            object targetValue;
            if (propertyType.IsNullableEnum() || propertyType.IsEnum)
            {
                var enumConverter = (EnumConverter) typeConverter;
                targetValue = enumConverter.Convert(propertyType, newValue);
            }
            else
            {
                targetValue = typeConverter.Convert(newValue);
            }

            var setter = GetOrCreateSetter(instance.GetType(), propertyInfo);
            setter(instance, targetValue);
        }

        internal static Func<object, object> GetOrCreateGetter(Type sourceType, PropertyInfo propertyInfo)
        {
            Func<object, object> result;
            if (s_getters.TryGetValue(propertyInfo, out result) == false)
            {
                var instance = Expression.Parameter(typeof(object), "instance");

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
                result = lambda.Compile();
                s_getters.TryAdd(propertyInfo, result);
            }

            return result;
        }

        internal static Action<object, object> GetOrCreateSetter(Type sourceType, PropertyInfo propertyInfo)
        {
            Action<object, object> result;
            if (s_setters.TryGetValue(propertyInfo, out result) == false)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var value    = Expression.Parameter(typeof(object), "value");

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
                result = lambda.Compile();
                s_setters.TryAdd(propertyInfo, result);
            }

            return result;
        }
    }
}