using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Lab.DynamicAccessor.TypeConverter;

/// <summary>
/// The DynamicAccessors namespace.
/// </summary>

namespace Lab.DynamicAccessor
{
    public class MemberAccessor
    {
        private static readonly ConcurrentDictionary<Type, Action<object, string, object>> s_setters;

        private static readonly ConcurrentDictionary<Type, Func<object, string, object>> s_getters;

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object, object[], object>>>
            s_executes;

        private static readonly TypeConverterFactory s_typeConverterFactory;

        static MemberAccessor()
        {
            s_setters = new ConcurrentDictionary<Type, Action<object, string, object>>();
            s_getters = new ConcurrentDictionary<Type, Func<object, string, object>>();
            s_executes = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object, object[], object>>>();
            s_typeConverterFactory = new TypeConverterFactory();

            //s_ExecuteContainers = new Dictionary<string, Func<object, object[], object>>();
        }

        public virtual object Execute(object instance, MethodInfo methodInfo, object[] parameters)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            var execute = GetOrCreateExecute(instance, methodInfo);
            return execute(instance, parameters);
        }

        public virtual object Execute(object instance, string methodName, object[] parameters)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            var flags      = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var methodInfo = instance.GetType().GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                throw new Exception($"Not found method of '{methodName}'");
            }

            var execute = GetOrCreateExecute(instance, methodInfo);
            return execute(instance, parameters);
        }

        public virtual T GetValue<T>(object instance, PropertyInfo propertyInfo)
        {
            var getter = this.GetValue(instance, propertyInfo);
            return (T) getter;
        }

        public virtual T GetValue<T>(object instance, string propertyName)
        {
            var getter = this.GetValue(instance, propertyName);
            return (T) getter;
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

            var getter = GetOrCreateGetter(instance);
            return getter(instance, propertyInfo.Name);
        }

        public virtual object GetValue(object instance, string propertyName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (this.IsExistProperty(instance, propertyName) == false)
            {
                throw new Exception($"Not found property of '{propertyName}'");
            }

            var getter = GetOrCreateGetter(instance);
            return getter(instance, propertyName);
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

            var setter = GetOrCreateSetter(instance);
            setter(instance, propertyInfo.Name, targetValue);
        }

        //public virtual void SetValue(object instance, string propertyName, object newValue)
        //{
        //    if (instance == null)
        //    {
        //        throw new ArgumentNullException(nameof(instance));
        //    }

        //    if (string.IsNullOrWhiteSpace(propertyName))
        //    {
        //        throw new ArgumentNullException(nameof(propertyName));
        //    }

        //    if (this.IsExistProperty(instance, propertyName) == false)
        //    {
        //        throw new Exception($"Not found property of '{propertyName}'");
        //    }

        //    var setter = GetOrCreateSetter(instance);
        //    setter(instance, propertyName, newValue);
        //}

        /// <summary>
        ///     產生單一Method
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        private static Func<object, object[], object> CreateExecute(MethodInfo methodInfo)
        {
            Func<object, object[], object> result = null;

            // parameters to execute
            var instanceParameter   = Expression.Parameter(typeof(object),   "instance");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // build parameter list
            var parameterExpressions = new List<Expression>();
            var paramInfos           = methodInfo.GetParameters();
            for (var i = 0; i < paramInfos.Length; i++)
            {
                // (Ti)parameters[i]
                var valueObj  = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfos[i].ParameterType);
                parameterExpressions.Add(valueCast);
            }

            // non-instance for static method, or ((TInstance)instance)
            Expression instanceCast = methodInfo.IsStatic
                                          ? null
                                          : Expression.Convert(instanceParameter, methodInfo.ReflectedType);

            // static invoke or ((TInstance)instance).Method
            var methodCall = Expression.Call(
                                             instanceCast, methodInfo, parameterExpressions);

            // ((TInstance)instance).Method((T0)parameters[0], (T1)parameters[1], ...)
            if (methodCall.Type == typeof(void))
            {
                var lambda =
                    Expression.Lambda<Action<object, object[]>>(
                                                                methodCall, instanceParameter, parametersParameter);

                var compile = lambda.Compile();

                result = (instance, parameters) =>
                         {
                             compile(instance, parameters);
                             return null;
                         };
            }
            else
            {
                var castMethodCall = Expression.Convert(
                                                        methodCall, typeof(object));
                var lambda =
                    Expression.Lambda<Func<object, object[], object>>(
                                                                      castMethodCall, instanceParameter,
                                                                      parametersParameter);
                result = lambda.Compile();
            }

            return result;
        }

        /// <summary>
        ///     由快取裡面取得Method或是新建立
        /// </summary>
        /// <param name="sourceInstance"></param>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        private static Func<object, object[], object> GetOrCreateExecute(object sourceInstance, MethodInfo methodInfo)
        {
            var sourceType = sourceInstance.GetType();
            var methodName = methodInfo.Name;

            ConcurrentDictionary<string, Func<object, object[], object>> executors = null;
            Func<object, object[], object>                               result    = null;
            if (s_executes.TryGetValue(sourceType, out executors) == false)
            {
                executors = new ConcurrentDictionary<string, Func<object, object[], object>>();
                result    = CreateExecute(methodInfo);
                executors.TryAdd(methodName, result);
                s_executes.TryAdd(sourceType, executors);
            }
            else
            {
                if (executors.TryGetValue(methodName, out result) == false)
                {
                    result = CreateExecute(methodInfo);
                    executors.TryAdd(methodName, result);
                }
            }

            return result;
        }

        /// <summary>
        ///     從快取裡面取得或是建立所有Getter
        /// </summary>
        /// <param name="sourceInstance"></param>
        /// <returns></returns>
        private static Func<object, string, object> GetOrCreateGetter(object sourceInstance)
        {
            var                          sourceType = sourceInstance.GetType();
            Func<object, string, object> result     = null;
            if (s_getters.TryGetValue(sourceType, out result) == false)
            {
                var instance        = Expression.Parameter(typeof(object), "instance");
                var memberName      = Expression.Parameter(typeof(string), "memberName");
                var nameHash        = Expression.Variable(typeof(int), "nameHash");
                var callGetHashCode = Expression.Call(memberName, typeof(object).GetMethod("GetHashCode"));

                var assignHashCode = Expression.Assign(nameHash, callGetHashCode);
                var instanceCast = Expression.Convert(instance, sourceType);
                var cases = new List<SwitchCase>();
                var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                var propertyInfos = sourceType.GetProperties(flag);
                foreach (var propertyInfo in propertyInfos)
                {
                    MemberExpression property;
                    if (propertyInfo.IsStatic())
                    {
                        property = Expression.Property(null, propertyInfo);
                    }
                    else
                    {
                        property = Expression.Property(instanceCast, propertyInfo);
                    }

                    var hashCode     = Expression.Constant(propertyInfo.Name.GetHashCode(), typeof(int));
                    var propertyCast = Expression.Convert(property, typeof(object));
                    cases.Add(Expression.SwitchCase(propertyCast, hashCode));
                }

                var switchEx   = Expression.Switch(nameHash, Expression.Constant(null), cases.ToArray());
                var methodBody = Expression.Block(typeof(object), new[] {nameHash}, assignHashCode, switchEx);
                var lambda     = Expression.Lambda<Func<object, string, object>>(methodBody, instance, memberName);
                result = lambda.Compile();
                s_getters.TryAdd(sourceType, result);
            }

            return result;
        }

        /// <summary>
        ///     從快取裡面取得或是建立所有Setter
        /// </summary>
        /// <param name="sourceInstance"></param>
        /// <returns></returns>
        private static Action<object, string, object> GetOrCreateSetter(object sourceInstance)
        {
            var                            sourceType = sourceInstance.GetType();
            Action<object, string, object> result;
            if (s_setters.TryGetValue(sourceType, out result) == false)
            {
                var instance   = Expression.Parameter(typeof(object), "instance");
                var memberName = Expression.Parameter(typeof(string), "memberName");
                var newValue   = Expression.Parameter(typeof(object), "newValue");
                var nameHash   = Expression.Variable(typeof(int), "nameHash");
                var getHashCodeMethod = Expression.Call(memberName,
                                                        typeof(object).GetMethod("GetHashCode"));
                var assignHash = Expression.Assign(nameHash, getHashCodeMethod);
                var cases      = new List<SwitchCase>();

                var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                var propertyInfos = sourceType.GetProperties(flag);
                var instanceCast = Expression.Convert(instance, sourceType);
                foreach (var propertyInfo in propertyInfos)
                {
                    MemberExpression property;
                    if (propertyInfo.IsStatic())
                    {
                        property = Expression.Property(null, propertyInfo);
                    }
                    else
                    {
                        property = Expression.Property(instanceCast, propertyInfo);
                    }

                    var hashCode     = Expression.Constant(propertyInfo.Name.GetHashCode(), typeof(int));
                    var newValueCast = Expression.Convert(newValue, propertyInfo.PropertyType);
                    var assign       = Expression.Assign(property, newValueCast);
                    var assignCast   = Expression.Convert(assign, typeof(object));
                    var switchCase   = Expression.SwitchCase(assignCast, hashCode);
                    cases.Add(switchCase);
                }

                var switchEx   = Expression.Switch(nameHash, Expression.Constant(null), cases.ToArray());
                var methodBody = Expression.Block(typeof(object), new[] {nameHash}, assignHash, switchEx);
                var lambda =
                    Expression.Lambda<Action<object, string, object>>(methodBody, instance, memberName, newValue);
                result = lambda.Compile();
                s_setters.TryAdd(sourceType, result);
            }

            return result;
        }

        private bool IsExistProperty(object instance, string propertyName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var propertyInfo = instance.GetType().GetProperty(propertyName, flags);

            if (propertyInfo == null)
            {
                return false;
            }

            return true;
        }
    }
}