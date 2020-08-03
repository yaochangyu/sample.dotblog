using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.DyanmiceAccessor
{
    public interface IFieldAccessor
    {
        object GetValue(object instance);
    }

    public class FieldAccessor : IFieldAccessor
    {
        private Func<object, object> m_getter;

        public FieldInfo FieldInfo { get; private set; }

        public FieldAccessor(FieldInfo fieldInfo)
        {
            this.FieldInfo = fieldInfo;
            this.m_getter = this.GetDelegate(fieldInfo);
        }

        private Func<object, object> GetDelegate(FieldInfo fieldInfo)
        {
            // target: (object)(((TInstance)instance).Field)

            // preparing parameter, object type
            var instance = Expression.Parameter(typeof(object), "instance");

            // non-instance for static method, or ((TInstance)instance)
            var instanceCast = fieldInfo.IsStatic ? null :
                Expression.Convert(instance, fieldInfo.ReflectedType);

            // ((TInstance)instance).Property
            var fieldAccess = Expression.Field(instanceCast, fieldInfo);

            // (object)(((TInstance)instance).Property)
            var castFieldValue = Expression.Convert(fieldAccess, typeof(object));

            // Lambda expression
            var lambda = Expression.Lambda<Func<object, object>>(castFieldValue, instance);

            return lambda.Compile();
        }

        public object GetValue(object instance)
        {
            return this.m_getter(instance);
        }

        #region IFieldAccessor Members

        object IFieldAccessor.GetValue(object instance)
        {
            return this.GetValue(instance);
        }

        #endregion
    }
}
