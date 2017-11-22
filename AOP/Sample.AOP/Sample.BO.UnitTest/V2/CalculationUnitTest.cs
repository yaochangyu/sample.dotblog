using System;
using System.Security.Principal;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.BO.V2;

namespace Sample.BO.UnitTest.V2
{
    [TestClass]
    public class CalculationUnitTest
    {
        [TestMethod]
        public void 調用Execute_預期得到例外2()
        {
            //Thread.CurrentPrincipal =
            //    new GenericPrincipal(new GenericIdentity("Administrator"),
            //                         new[] { "ADMIN" });
            ICalculation calculation = CalculationFactory.Create();
            calculation.Execute(1, 1);
            Action action = () => { calculation.Execute(1, 1); };

            action.ShouldThrow<Exception>()
                  .WithInnerException<Exception>().WithInnerMessage("喔喔*");
        }
    }
}