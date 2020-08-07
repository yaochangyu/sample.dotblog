using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.DynamicAccessor
{
    public interface IFieldAccessor
    {
        object GetValue(object instance);

        void SetValue(object instance, object value);
    }

    public class FieldAccessor : IFieldAccessor
    {
        public FieldInfo FieldInfo { get; }

        private readonly Func<object, object>   _getter;
        private readonly Action<object, object> _setter;

        public FieldAccessor(FieldInfo fieldInfo)
        {
            this.FieldInfo = fieldInfo;
            this._getter   = this.CreateGetter(fieldInfo);
            this._setter   = this.CreateSetter(fieldInfo);
        }

        public object GetValue(object instance)
        {
            return this._getter(instance);
        }

        public void SetValue(object instance, object value)
        {
            this._setter(instance, value);
        }

        object IFieldAccessor.GetValue(object instance)
        {
            return this.GetValue(instance);
        }

        internal Func<object, object> CreateGetter(FieldInfo fieldInfo)
        {
            var              instance   = Expression.Parameter(typeof(object), "instance");
            var              sourceType = fieldInfo.ReflectedType;
            MemberExpression property;
            if (fieldInfo.IsStatic)
            {
                property = Expression.Field(null, fieldInfo);
            }
            else
            {
                var instanceCast = Expression.Convert(instance, sourceType);

                //property = Expression.Property(instanceCast, propertyInfo.Name);

                property = Expression.Field(instanceCast, fieldInfo);
            }

            var propertyCast = Expression.Convert(property, typeof(object));

            var lambda = Expression.Lambda<Func<object, object>>(propertyCast, instance);
            var result = lambda.Compile();

            return result;
        }

        internal Action<object, object> CreateSetter(FieldInfo fieldInfo)
        {
            var              instance   = Expression.Parameter(typeof(object), "instance");
            var              value      = Expression.Parameter(typeof(object), "value");
            var              sourceType = fieldInfo.DeclaringType;
            MemberExpression property;
            if (fieldInfo.IsStatic)
            {
                property = Expression.Field(null, fieldInfo);
            }
            else
            {
                var instanceCast = Expression.Convert(instance, sourceType);

                //property = Expression.Property(instanceCast, propertyInfo.Name);

                property = Expression.Field(instanceCast, fieldInfo);
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