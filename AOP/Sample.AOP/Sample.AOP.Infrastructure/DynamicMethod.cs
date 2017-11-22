using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sample.AOP.Infrastructure
{
    public static class DynamicMethod
    {
        private static readonly Dictionary<string, Func<object, object[], object>> s_executeContainers;

        static DynamicMethod()
        {
            if (s_executeContainers == null)
            {
                s_executeContainers = new Dictionary<string, Func<object, object[], object>>();
            }
        }

        public static object Execute(object instance, Expression<Action> member)
        {
            var callbackParameters = GetCallbackParameters(member);
            var execute = BuildMethod(member);
            var result = execute(instance, callbackParameters);
            return result;
        }

        public static Func<object, object[], object> BuildMethod(Expression<Action> member)
        {
            var body = (MethodCallExpression) member.Body;
            var methodInfo = body.Method;
            return BuildMethod(methodInfo);
        }

        public static Func<object, object[], object> BuildMethod(MethodInfo methodInfo)
        {
            var sourceType = methodInfo.DeclaringType;
            var key = string.Format("{0}.{1}", sourceType.FullName, methodInfo.Name);
            Func<object, object[], object> executer;

            if (!s_executeContainers.ContainsKey(key))
            {
                // parameters to execute
                var instanceParameter =
                    Expression.Parameter(typeof(object), "instance");
                var parametersParameter =
                    Expression.Parameter(typeof(object[]), "parameters");

                // build parameter list
                var parameterExpressions = new List<Expression>();
                var paramInfos = methodInfo.GetParameters();
                for (var i = 0; i < paramInfos.Length; i++)
                {
                    // (Ti)parameters[i]
                    var valueObj = Expression.ArrayIndex(parametersParameter,
                                                         Expression.Constant(i));
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

                    executer = (instance, parameters) =>
                               {
                                   compile(instance, parameters);
                                   return null;
                               };
                    s_executeContainers.Add(key, executer);
                }
                else
                {
                    var castMethodCall = Expression.Convert(methodCall, typeof(object));
                    var lambda = Expression.Lambda<Func<object, object[], object>>(castMethodCall, instanceParameter,
                                                                                   parametersParameter);
                    executer = lambda.Compile();
                    s_executeContainers.Add(key, executer);
                }
            }
            else
            {
                executer = s_executeContainers[key];
            }

            return executer;
        }

        public static TTarget GetCallbackCustomAttribute<TTarget>(object sourceInstance,
                                                                  Expression<Action> sourceMember)
            where TTarget : Attribute
        {
            var result = default(TTarget);
            var targetType = typeof(TTarget);

            //sourceMember參數所拿到的instance不是我要的，所以另外再傳入instance
            var sourceType = sourceInstance.GetType();
            var body = (MethodCallExpression) sourceMember.Body;

            var callbackMethod = body.Method;
            var sourceMethod = sourceType.GetMethod(callbackMethod.Name);

            object filterAttribute = sourceMethod.GetCustomAttributes()
                                                 .FirstOrDefault(p => p.GetType() == targetType);
            result = (TTarget) filterAttribute;

            return result;
        }

        public static object[] GetCallbackParameters(Expression<Action> member)
        {
            object[] result = null;
            var body = (MethodCallExpression) member.Body;
            var parameters = member.Parameters;
            result = body.Arguments
                         .Select(p =>
                                 {
                                     var lambda = Expression.Lambda(p, parameters);
                                     var compile = lambda.Compile();
                                     var value = compile.DynamicInvoke();
                                     return value;
                                 }).ToArray();
            return result;
        }

        //public static void InvokeNoParameter<TSource>(TSource source, Expression<Action<TSource>> member)
        //{
        //    var info = (MethodCallExpression) member.Body;
        //    var methodName = info.Method.Name;

        //    var sourceType = source.GetType();
        //    var methodInfo = sourceType.GetMethod(methodName);

        //    var attribute = methodInfo.GetCustomAttribute<ExceptionNotityAttribute>();
        //    try
        //    {
        //        var execute = GenerateExecute<TSource>(methodInfo);
        //        execute(source, null);
        //    }
        //    catch (Exception e)
        //    {
        //        if (attribute != null)
        //        {
        //            attribute.CatchException(e);
        //        }

        //        throw new AOPException("AOP Exception", e);
        //    }
        //}
    }
}