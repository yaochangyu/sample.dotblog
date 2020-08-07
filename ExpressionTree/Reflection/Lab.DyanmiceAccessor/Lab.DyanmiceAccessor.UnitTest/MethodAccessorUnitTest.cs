using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DynamicAccessor.UnitTest
{
    [TestClass]
    public class MethodAccessorUnitTest
    {
        [TestMethod]
        public void 執行Sum方法()
        {
            var instance   = new MyClass();
            var methodInfo = instance.GetType().GetMethod("Sum");
            var accessor   = new MethodAccessor(methodInfo);
            var result     = accessor.Execute(instance,  1, 1);
            Assert.AreEqual(2, result);
        }
        private class MyClass
        {
            public int Sum(int p1, int p2)
            {
                return p1 + p2;
            }
        }
    }
}