using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Lab.DynamicAccessor
{
    public class ConstructorAccessor : IConstructorAccessor
    {
        public ConstructorInfo ConstructorInfo { get; set; }

        private readonly Func<object[], object> _execute;

        public ConstructorAccessor(ConstructorInfo constructorInfo)
        {
            this.ConstructorInfo = constructorInfo;
            this._execute        = this.CreateExecuter(constructorInfo);
        }

        public object Execute(params object[] parameters)
        {
            return this._execute(parameters);
        }

        private Func<object[], object> CreateExecuter(ConstructorInfo constructorInfo)
        {
            // Target: (object)new T((T0)parameters[0], (T1)parameters[1], ...)

            // parameters to execute
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

            // build parameter list
            var parameterExpressions = new List<Expression>();
            var paramInfos           = constructorInfo.GetParameters();
            for (var i = 0; i < paramInfos.Length; i++)
            {
                // (Ti)parameters[i]
                var valueObj  = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                var valueCast = Expression.Convert(valueObj, paramInfos[i].ParameterType);

                parameterExpressions.Add(valueCast);
            }

            // new T((T0)parameters[0], (T1)parameters[1], ...)
            var instanceCreate = Expression.New(constructorInfo, parameterExpressions);

            // (object)new T((T0)parameters[0], (T1)parameters[1], ...)
            var instanceCreateCast = Expression.Convert(instanceCreate, typeof(object));

            var lambda = Expression.Lambda<Func<object[], object>>(instanceCreateCast, parametersParameter);

            return lambda.Compile();
        }
    }
}