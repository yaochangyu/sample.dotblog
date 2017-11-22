using System;
using System.Security.Principal;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.BO.V0;

namespace Sample.BO.UnitTest.V0
{
    [TestClass]
    public class CalculationUnitTest
    {
        [TestMethod]
        public void V0_通過驗證且不是Admin角色()
        {
            var calculation = new Calculation();

            Action action = () => { calculation.Execute(1, 1); };
            action.ShouldThrow<Exception>().WithMessage("*Admin*");
        }

        [TestMethod]
        public void V0_通過驗證且是Admin角色_調用Execute_預期得到例外()
        {
            Thread.CurrentPrincipal =
                new GenericPrincipal(new GenericIdentity("Administrator"),
                                     new[] {"Admin"});
            var calculation = new Calculation();

            Action action = () => { calculation.Execute(1, 1); };
            action.ShouldThrow<Exception>().WithMessage("*喔喔*");
        }
    }
}