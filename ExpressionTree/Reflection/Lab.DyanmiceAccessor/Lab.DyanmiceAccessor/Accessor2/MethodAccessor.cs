using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.DynamicAccessor.Accessor2
{
    public class MethodAccessor : IMethodAccessor
    {
        public MethodInfo MethodInfo { get; set; }

        private readonly Func<object, object[], object> _executer;

        public MethodAccessor(MethodInfo methodInfo)
        {
            this.MethodInfo = methodInfo;
            this._executer  = CreateExecuter(methodInfo);
        }

        public object Execute(object instance, params object[] parameters)
        {
            return this._executer.Invoke(instance, parameters);
        }

        /// <summary>
        ///     產生單一Method
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        private static Func<object, object[], object> CreateExecuter(MethodInfo methodInfo)
        {
            Func<object, object[], object> result;

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
                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambda =
                    Expression.Lambda<Func<object, object[], object>>(
                                                                      castMethodCall, instanceParameter,
                                                                      parametersParameter);
                result = lambda.Compile();
            }

            return result;
        }
    }
}