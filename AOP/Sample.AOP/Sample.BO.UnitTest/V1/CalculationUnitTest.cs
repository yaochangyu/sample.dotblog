using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.BO.V1;

namespace Sample.BO.UnitTest.V1
{
    [TestClass]
    public class CalculationUnitTest
    {
        [TestMethod]
        public void 調用Execute_預期得到例外()
        {
            var calculation = new CalculationProxy();
            Action action = () => { calculation.Execute(1, 1); };
            action.ShouldThrow<Exception>().WithMessage("喔喔，出錯了");
        }
    }
}
