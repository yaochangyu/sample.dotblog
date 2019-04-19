using System;
using ClassLibrary1;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var calculation = new Calculation();
            var actual = calculation.Add(1, 1);
            Assert.AreEqual(2,actual);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var calculation = new Calculation();
            var actual = calculation.Add(1, 1);
            Assert.AreEqual(1,actual);
        }
    }
}
