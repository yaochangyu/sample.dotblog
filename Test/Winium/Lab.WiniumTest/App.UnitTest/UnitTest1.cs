using System;
using System.Text;
using Castle.DynamicProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace App.UnitTest
{
    public interface ICalculator    
    {    
        double Add(double x, double y);    
        double Divide(double x, double y);    
    }    
    public class Calculator: ICalculator    
    {    
        public double Add(double x, double y)    
        {    
            return x + y;    
        }    
        public double Divide(double x, double y)    
        {    
            return x / y;    
        }    
    }  
    public class LogInterceptor: IInterceptor    
    {    
        void IInterceptor.Intercept(IInvocation invocation)    
        {    
            var logger = LogManager.GetLogger("");    
            try    
            {    
                StringBuilder sb = null;    
                if (logger.IsDebugEnabled)    
                {    
                    sb = new StringBuilder(invocation.TargetType.FullName).Append(".").Append(invocation.Method).Append("("); //getting method name    
                    //getting parameters    
                    for (int i = 0; i < invocation.Arguments.Length; i++)    
                    {    
                        if (i > 0) sb.Append(", ");    
                        sb.Append(invocation.Arguments[i]);    
                    }    
                    sb.Append(")");    
                    logger.Debug(sb);    
                }    
                invocation.Proceed();    
                if (logger.IsDebugEnabled)    
                {    
                    //getting return    
                    logger.Debug("Return of " + sb + " is: " + invocation.ReturnValue);    
                }    
            }    
            catch (Exception e)    
            {    
                logger.Error(e);    
                throw;    
            }    
        }    
    }   
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // client call.
            var calculateObject = ProxyFactory.GetProxyService(
                                                               typeof(ICalculation), typeof(LogAspect), new Calculation());
            calculateObject.Calculate(1, 2);

        }
    }

}
